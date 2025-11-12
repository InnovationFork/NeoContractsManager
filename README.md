<img width="1373" height="493" alt="image" src="https://github.com/user-attachments/assets/0f135c2f-f3de-49c7-8e91-134f9f22af98" />


# NeoContractsManager Documentation

## Overview

NeoContractsManager is a Windows Forms application for deploying, updating, and destroying Neo N3 smart contracts. It provides a user-friendly interface for managing multiple contract deployments across different Neo networks.

## Core Operations

### Deploy

Deploys new smart contracts to the Neo blockchain.

**Process:**
1. Reads `.nef` and `.manifest.json` files
2. Creates deployment transaction using `ContractClient.CreateDeployContractTxAsync`
3. Signs transaction with provided WIF key
4. Broadcasts to network via RPC
5. Waits for confirmation and retrieves application log
6. Stores transaction hash and contract hash for future operations

**Contract Hash Calculation:**
```csharp
UInt160 contractHash = Helper.GetContractHash(sender, nefFile.CheckSum, manifest.Name);
```

### Update

Updates existing smart contracts by invoking the `update` method.

**Process:**
1. Validates contract hash (0x-prefixed, 40 hex characters)
2. Loads new `.nef` bytes and `.manifest.json` text
3. Invokes `update(nef, manifest, data?)` method via `InvokeSC_WithWIF`
4. Waits for transaction confirmation
5. Returns VM state and gas consumed

**Parameters:**
- `nef`: ByteArray - compiled contract bytecode
- `manifest`: String - contract manifest JSON
- `data`: Any (optional) - additional update data

### Destroy

Permanently removes smart contracts from the blockchain by invoking the `destroy` method.

**Process:**
1. Prompts for contract hash (0x format)
2. Invokes `destroy()` method with no parameters
3. Waits for confirmation
4. Returns final VM state and gas consumed

## Transaction Building with InvokeSC_WithWIF

The `InvokeSC_WithWIF` method provides a unified interface for contract invocations with automatic fee calculation and transaction signing.

### Method Signature

```csharp
public async Task<string?> InvokeSC_WithWIF(
    string rpcAddress, 
    UInt160 contractScriptHash, 
    string methodName, 
    ContractParameter[] parameters, 
    string wif, 
    WitnessScope scope = WitnessScope.CalledByEntry, 
    long extraSystemFee = 0, 
    long extraNetworkFee = 0, 
    CancellationToken cancellationToken = default)
```

### Internal Flow

1. **Script Building**
   ```csharp
   using ScriptBuilder sb = new();
   sb.EmitDynamicCall(contractScriptHash, methodName, parameters);
   byte[] script = sb.ToArray();
   ```

2. **Signer Configuration**
   ```csharp
   KeyPair keyPair = Utility.GetKeyPair(wif);
   UInt160 account = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
   
   var signers = new[] {
       new Signer {
           Scopes = scope,
           Account = account
       }
   };
   ```

3. **Transaction Creation via MakeTransactionAsync**
   ```csharp
   var tmFactory = new TransactionManagerFactory(rpcClient);
   var txManager = await tmFactory.MakeTransactionAsync(script, signers);
   ```

   **MakeTransactionAsync** performs:
   - Script execution simulation
   - System fee calculation (based on gas consumed)
   - Network fee calculation (based on transaction size and verification costs)
   - Witness scope validation
   - Attribute attachment if needed

4. **Signing and Broadcasting**
   ```csharp
   var tx = await txManager.AddSignature(keyPair).SignAsync();
   await rpcClient.SendRawTransactionAsync(tx);
   ```

### Fee Calculation

Fees are automatically calculated by the Neo RPC node:
- **System Fee**: Gas consumed during contract execution
- **Network Fee**: Based on transaction size + signature verification cost

The `extraSystemFee` and `extraNetworkFee` parameters allow manual fee adjustments if needed.

## Configuration Persistence

### Auto-save Behavior

The application automatically saves configuration to `n3cmanconfig.json` after:
- Adding/removing contracts
- Successful deployments
- Successful updates

### Configuration Structure

```json
{
  "RpcAddress": "https://testnet2.neo.coz.io:443",
  "Wif": "your-wif-key-here",
  "Contracts": [
    {
      "NefPath": "path/to/contract.nef",
      "ManifestPath": "path/to/contract.manifest.json",
      "Selected": true,
      "LastDeployTx": "0x...",
      "ContractHash": "0x..."
    }
  ]
}
```

### ?? Security Warning

**WIF keys are stored in CLEARTEXT in the configuration file.**

- The configuration file contains your private key in unencrypted form
- Anyone with file access can retrieve your private key
- **Never commit configuration files containing WIF keys to version control**
- **Never share configuration files with WIF keys**
- Consider using environment variables or secure key storage for production use

**Recommended practices:**
- Add `n3cmanconfig.json` to `.gitignore`
- Use test wallets with minimal funds for development
- Clear WIF field before saving if sharing configuration
- Use dedicated deployment wallets separate from main accounts

## Network Selection

Supported networks (via protocol settings):
- **TestNet** (`config.testnet.json`)
- **MainNet** (`config.mainnet.json`)
- **PrivNet** (`config.privnet.json`)

Network selection affects:
- Protocol magic number
- Address version
- System fee calculation
- Network fee calculation

## Contract Hash Management

Contract hashes are:
- Auto-calculated during deployment
- Stored in configuration for reuse in updates
- Normalized to `0x` + 40 lowercase hex characters
- Validated before update/destroy operations

**Hash format:** `0x` followed by 40 hexadecimal characters (160 bits)

## Error Handling

All operations report:
- **VM State**: `HALT` (success) or `FAULT` (failure)
- **Exception details**: Extracted from application log if available
- **Gas consumed**: Actual gas used by transaction
- **Transaction hash**: For blockchain explorer lookup

## Dependencies

- **Neo SDK**: v3.x
- **Neo.Network.RPC**: For blockchain interaction
- **System.Text.Json**: Configuration serialization
- **.NET Framework/Core**: Windows Forms runtime

## Usage Example

1. **Add Contracts**: Select folder containing `.nef` and `.manifest.json` files
2. **Configure RPC**: Set network RPC endpoint
3. **Provide WIF**: Enter wallet WIF private key
4. **Select Operations**: Check contracts and click Deploy/Update/Destroy
5. **Monitor Results**: View status, transaction hashes, and VM states in the list

## License

See LICENSE file for details.
