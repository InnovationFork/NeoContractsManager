using Neo;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace NeoContractsManager
{
    public partial class NeoContractsManager_Main : Form
    {
        private class ContractEntry
        {
            public string NefPath { get; init; } = string.Empty;
            public string? ManifestPath { get; init; }
            public long NefSize { get; init; }
            public long ManifestSize { get; init; }
            // New: store deploy tx and contract hash
            public string? LastDeployTx { get; set; }
            public string? ContractHash { get; set; }
        }

        public NeoContractsManager_Main()
        {
            InitializeComponent();
        }


        private async void button_Deploy_Click(object sender, EventArgs e)
        {

            DialogResult dresult = MessageBox.Show("Are you sure you want to deploy the selected contracts?", "Confirm Deploy", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dresult != DialogResult.Yes) return;



            var rpc = textBox_RPCAddress.Text?.Trim();
            var wif = textBox_WIF.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rpc) || string.IsNullOrWhiteSpace(wif))
            {
                MessageBox.Show("Specify RPC URL and WIF", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToggleUi(false);
            try
            {
                bool anyChanged = false;
                foreach (ListViewItem item in listView_ContractsFilesList.Items)
                {
                    if (!item.Checked) continue;
                    if (item.Tag is not ContractEntry entry)
                        continue;
                    if (string.IsNullOrEmpty(entry.NefPath) || string.IsNullOrEmpty(entry.ManifestPath) ||
                        !File.Exists(entry.NefPath) || !File.Exists(entry.ManifestPath))
                    {
                        SetItemStatus(item, "Missing files", "");
                        continue;
                    }

                    SetItemStatus(item, "Deploying...", "");
                    try
                    {
                        var (txHash, vmState, contractHash, error) = await DeployAsync(rpc!, wif!, entry.NefPath, entry.ManifestPath!).ConfigureAwait(true);

                        // persist in entry
                        entry.LastDeployTx = txHash;
                        entry.ContractHash = contractHash;

                        // update list columns: status (4), tx (5), hash (resolved)
                        if (!string.IsNullOrEmpty(error) || (vmState?.IndexOf("FAULT", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            SetItemStatus(item, string.IsNullOrEmpty(error) ? "FAULT" : error!, $"Tx: {txHash} | VM: {vmState}");
                        }
                        else
                        {
                            SetItemStatus(item, "OK", $"Tx: {txHash} | VM: {vmState}");
                        }
                        SetItemHash(item, contractHash);

                        anyChanged = true;
                    }
                    catch (Exception ex)
                    {
                        SetItemStatus(item, ex.Message, string.Empty);
                    }
                }

                // Save default config snapshot if anything changed
                if (anyChanged)
                {
                    TrySaveDefaultConfigSnapshot();
                }
            }
            finally
            {
                ToggleUi(true);
            }
        }

        private async void button_DestroySelected_Click(object sender, EventArgs e)
        {
            DialogResult dresult = MessageBox.Show("Are you sure you want to destroy the selected contracts?", "Confirm Destoy", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dresult != DialogResult.Yes) return;


            var rpc = textBox_RPCAddress.Text?.Trim();
            var wif = textBox_WIF.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rpc) || string.IsNullOrWhiteSpace(wif))
            {
                MessageBox.Show("Specify RPC URL and WIF", "Destroy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToggleUi(false);
            try
            {
                bool anyChanged = false;

                // 1) ”бедитьс€ что у всех выбранных элементов есть хеш (как в Update)
                if (!EnsureHashesForSelectedItems())
                {
                    ToggleUi(true);
                    return;
                }

                // 2) ¬ыполнить destroy дл€ каждого элемента, использу€ его хеш через GetItemContractHash
                foreach (ListViewItem item in listView_ContractsFilesList.Items)
                {
                    if (!item.Checked) continue;

                    var rawHash = GetItemContractHash(item);
                    if (string.IsNullOrWhiteSpace(rawHash) || !TryNormalizeContractHash(rawHash!, out var normalizedHash))
                    {
                        SetItemStatus(item, "Invalid/empty hash", rawHash ?? string.Empty);
                        continue;
                    }

                    // —охранить нормализованный в модель и UI
                    if (item.Tag is ContractEntry entry)
                    {
                        entry.ContractHash = normalizedHash;
                    }
                    SetItemHash(item, normalizedHash);

                    SetItemStatus(item, "Invoking destroy...", "");
                    try
                    {
                        var result = await InvokeDestroyAsync(rpc!, wif!, normalizedHash!).ConfigureAwait(true);
                        SetItemStatus(item, result.state ?? "Invoked", $"Gas: {result.gasConsumed}");
                        anyChanged = true;
                    }
                    catch (Exception ex)
                    {
                        SetItemStatus(item, "Error", ex.Message);
                    }
                }

                if (anyChanged)
                {
                    TrySaveDefaultConfigSnapshot();
                }
            }
            finally
            {
                ToggleUi(true);
            }
        }

        private async void button_UpdateSelected_Click(object sender, EventArgs e)
        {
            DialogResult dresult = MessageBox.Show("Are you sure you want to update the selected contracts?", "Confirm Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dresult != DialogResult.Yes) return;


            var rpc = textBox_RPCAddress.Text?.Trim();
            var wif = textBox_WIF.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rpc) || string.IsNullOrWhiteSpace(wif))
            {
                MessageBox.Show("Specify RPC URL and WIF", "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Do not prompt by default for optional data; leave null unless you add UI to supply it
            string? optionalData = null;

            ToggleUi(false);
            try
            {
                bool anyChanged = false;

                // 1) Ensure hashes exist for all selected items; if any missing, prompt once and apply
                if (!EnsureHashesForSelectedItems())
                {
                    // user cancelled or invalid input handled per item
                    ToggleUi(true);
                    return;
                }

                // 2) Run updates
                foreach (ListViewItem item in listView_ContractsFilesList.Items)
                {
                    if (!item.Checked) continue;
                    if (item.Tag is not ContractEntry entry)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(entry.NefPath) || string.IsNullOrEmpty(entry.ManifestPath) ||
                        !File.Exists(entry.NefPath) || !File.Exists(entry.ManifestPath))
                    {
                        SetItemStatus(item, "Missing files", "");
                        continue;
                    }

                    var rawHash = GetItemContractHash(item);
                    if (string.IsNullOrWhiteSpace(rawHash) || !TryNormalizeContractHash(rawHash!, out var normalizedHash))
                    {
                        SetItemStatus(item, "Invalid/empty hash", rawHash ?? string.Empty);
                        continue;
                    }

                    // Persist normalized to entry and UI
                    entry.ContractHash = normalizedHash;
                    SetItemHash(item, normalizedHash);


                    SetItemStatus(item, "Invoking update...", "");
                    try
                    {
                        var result = await InvokeUpdateAsync(rpc!, wif!, normalizedHash!, entry.NefPath, entry.ManifestPath!, normalizedHash).ConfigureAwait(true);
                        SetItemStatus(item, result.state ?? "Invoked", $"Gas: {result.gasConsumed}");
                        anyChanged = true;
                    }
                    catch (Exception ex)
                    {
                        SetItemStatus(item, "Error", ex.Message);
                    }
                }

                if (anyChanged)
                {
                    TrySaveDefaultConfigSnapshot();
                }
            }
            finally
            {
                ToggleUi(true);
            }
        }

        private void button_RemoveContracts_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView_ContractsFilesList.CheckedItems)
            {
                listView_ContractsFilesList.Items.Remove(item);
            }
            TrySaveDefaultConfigSnapshot();
        }

        private void NeoContractsManager_Main_Load(object sender, EventArgs e)
        {
            listView_ContractsFilesList.View = View.Details;
            if (listView_ContractsFilesList.Columns.Count >= 7)
            {
                listView_ContractsFilesList.Columns[0].Width = 200; // Nef Contract
                listView_ContractsFilesList.Columns[1].Width = 80;  // Nef Size
                listView_ContractsFilesList.Columns[2].Width = 220; // Manifest file
                listView_ContractsFilesList.Columns[3].Width = 90;  // Manifest Size
                listView_ContractsFilesList.Columns[4].Width = 180; // Deploy Status
                listView_ContractsFilesList.Columns[5].Width = 300; // Tx Answer
                listView_ContractsFilesList.Columns[6].Width = 320; // Contract Hash
            }

            // Initialize network combo from available config files
            InitializeNetworkComboFromFiles();

            // Defaults
            textBox_RPCAddress.Text = "https://testnet2.neo.coz.io:443";

            // Autoload config if present
            try
            {
                var defaultConfig = GetDefaultConfigPath();
                if (File.Exists(defaultConfig))
                {
                    LoadConfigFromFile(defaultConfig);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeNetworkComboFromFiles()
        {
            try
            {
                comboBox_NeoNetowrk.Items.Clear();

                var candidates = new List<(string Key, string Display, string Relative)>
                {
                    ("test", "TestNet", @"..\..\..\config.testnet.json"),
                    ("main", "MainNet", @"..\..\..\config.mainnet.json"),
                    ("priv", "PrivNet", @"..\..\..\config.privnet.json")
                };

                var foundAny = false;
                foreach (var c in candidates)
                {
                    var full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, c.Relative));
                    if (File.Exists(full))
                    {
                        comboBox_NeoNetowrk.Items.Add(c.Display);
                        foundAny = true;
                    }
                }

                if (!foundAny)
                {
                    // fallback: add default TestNet entry even if file absent; LoadProtocolSettingsFromSelection will still map to test
                    comboBox_NeoNetowrk.Items.Add("TestNet");
                }

                // select default: prefer TestNet when present, else first item
                int defaultIndex = -1;
                for (int i = 0; i < comboBox_NeoNetowrk.Items.Count; i++)
                {
                    var text = comboBox_NeoNetowrk.Items[i]?.ToString()?.ToLowerInvariant() ?? string.Empty;
                    if (text.Contains("test")) { defaultIndex = i; break; }
                }
                if (defaultIndex < 0 && comboBox_NeoNetowrk.Items.Count > 0) defaultIndex = 0;
                if (defaultIndex >= 0) comboBox_NeoNetowrk.SelectedIndex = defaultIndex;
            }
            catch
            {
                // On any error, ensure at least TestNet exists as default
                if (comboBox_NeoNetowrk.Items.Count == 0) comboBox_NeoNetowrk.Items.Add("TestNet");
                if (comboBox_NeoNetowrk.SelectedIndex < 0) comboBox_NeoNetowrk.SelectedIndex = 0;
            }
        }

        public string[] GetAllNefFiles(string folderpath)
        {
            try
            {
                return Directory.GetFiles(folderpath, "*.nef", SearchOption.AllDirectories);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public string[] GetAllManifestFiles(string folderpath)
        {
            try
            {
                return Directory.GetFiles(folderpath, "*.manifest.json", SearchOption.AllDirectories);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }


        private void button_AddContracts_Click(object sender, EventArgs e)
        {

            using var fbd = new FolderBrowserDialog
            {
                Description = "Select a folder with .nef and .manifest.json files",
                ShowNewFolderButton = false,
                SelectedPath = AppContext.BaseDirectory
            };
            if (fbd.ShowDialog(this) != DialogResult.OK) return;
            var folderPath = fbd.SelectedPath;
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;
            var nefFiles = GetAllNefFiles(folderPath);
            var manifestFiles = GetAllManifestFiles(folderpath: folderPath);
            var allFiles = nefFiles.Concat(manifestFiles).ToArray();
            if (allFiles.Length == 0) return;
            List<ContractEntry> pairs = PairContracts(allFiles);
            foreach (var p in pairs)
            {
                var nefName = Path.GetFileName(p.NefPath);
                var manifestDisp = string.IsNullOrEmpty(p.ManifestPath) ? "<not found>" : Path.GetFileName(p.ManifestPath);
                ListViewItem item = new ListViewItem(nefName)
                {
                    Tag = p,
                    Checked = true
                };
                item.SubItems.Add(p.NefSize.ToString());
                item.SubItems.Add(manifestDisp);
                item.SubItems.Add(p.ManifestSize > 0 ? p.ManifestSize.ToString() : "0");
                item.SubItems.Add("Ready");
                EnsureSubItemCapacity(item, GetContractHashColumnIndex());
                item.SubItems[GetContractHashColumnIndex()].Text = p.ContractHash ?? string.Empty;
                listView_ContractsFilesList.Items.Add(item);
            }

            TrySaveDefaultConfigSnapshot();
        }

        private void listView_ContractsFilesList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ToggleUi(bool enabled)
        {
            button_AddContracts.Enabled = enabled;
            button_RemoveContracts.Enabled = enabled;
            button_Deploy.Enabled = enabled;
            button_DestroySelected.Enabled = enabled;
            button_UpdateSelected.Enabled = enabled;
        }

        private void SetItemStatus(ListViewItem item, string status, string txAnswer)
        {
            if (item.SubItems.Count >= 6)
            {
                item.SubItems[4].Text = status;
                item.SubItems[5].Text = txAnswer;
            }
        }

        private int GetContractHashColumnIndex()
        {
            // Try by Name first
            for (int i = 0; i < listView_ContractsFilesList.Columns.Count; i++)
            {
                var col = listView_ContractsFilesList.Columns[i];
                var name = col.Name?.ToLowerInvariant() ?? string.Empty;
                if (name.Contains("contracthash") || name.Contains("hash"))
                    return i;
            }
            // Try by header text
            for (int i = 0; i < listView_ContractsFilesList.Columns.Count; i++)
            {
                var text = listView_ContractsFilesList.Columns[i].Text?.ToLowerInvariant() ?? string.Empty;
                if (text.Contains("contract") && text.Contains("hash")) return i;
                if (text.Contains("hash")) return i;
                if (text.Contains("хеш")) return i; // RU retained for backward compatibility
            }
            // Fallback to last column if exists, otherwise 0
            return Math.Max(0, listView_ContractsFilesList.Columns.Count - 1);
        }

        private void EnsureSubItemCapacity(ListViewItem item, int targetIndex)
        {
            while (item.SubItems.Count <= targetIndex)
            {
                item.SubItems.Add(string.Empty);
            }
        }

        private void SetItemHash(ListViewItem item, string? contractHash)
        {
            if (item.Tag is ContractEntry ce)
            {
                ce.ContractHash = string.IsNullOrWhiteSpace(contractHash) ? null : contractHash;
            }

            var idx = GetContractHashColumnIndex();
            EnsureSubItemCapacity(item, idx);
            item.SubItems[idx].Text = contractHash ?? string.Empty;
        }

        private string? GetItemContractHash(ListViewItem item)
        {
            // Prefer the UI column value first (column resolved dynamically), then the model value
            var idx = GetContractHashColumnIndex();
            string? hash = item.SubItems.Count > idx ? item.SubItems[idx].Text : null;
            if (string.IsNullOrWhiteSpace(hash) && item.Tag is ContractEntry ce && !string.IsNullOrWhiteSpace(ce.ContractHash))
                hash = ce.ContractHash;
            return string.IsNullOrWhiteSpace(hash) ? null : hash!.Trim();
        }

        private static bool TryNormalizeContractHash(string input, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(input)) return false;
            var h = input.Trim();
            if (h.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) h = h[2..];
            if (h.Length != 40 || !h.All(Uri.IsHexDigit)) return false;
            normalized = "0x" + h.ToLowerInvariant();
            // Validate parsable
            try
            {
                _ = UInt160.Parse(normalized);
                return true;
            }
            catch
            {
                normalized = string.Empty;
                return false;
            }
        }

        private bool EnsureHashesForSelectedItems()
        {
            // Collect checked items missing hash
            var itemsMissing = new List<ListViewItem>();
            foreach (ListViewItem item in listView_ContractsFilesList.Items)
            {
                if (!item.Checked) continue;
                var val = GetItemContractHash(item);
                if (string.IsNullOrWhiteSpace(val))
                {
                    itemsMissing.Add(item);
                }
            }

            if (itemsMissing.Count == 0) return true;

            // Ask once for all missing
            var input = Prompt("Enter contract hash (0x...) Ч will be applied to all selected items without a hash", "Contract Hash");
            if (string.IsNullOrWhiteSpace(input))
            {
                foreach (var it in itemsMissing)
                    SetItemStatus(it, "No saved hash", "");
                return false;
            }

            if (!TryNormalizeContractHash(input!, out var normalized))
            {
                foreach (var it in itemsMissing)
                    SetItemStatus(it, "Invalid hash", input!);
                return false;
            }

            // Apply to all missing
            foreach (var it in itemsMissing)
            {
                SetItemHash(it, normalized);
                SetItemStatus(it, "Hash set", normalized);
            }
            return true;
        }

        private List<ContractEntry> PairContracts(string[] files)
        {
            static string MakeKeyForNef(string path)
            {
                var dir = Path.GetFullPath(Path.GetDirectoryName(path) ?? ".");
                var name = Path.GetFileNameWithoutExtension(path);
                return Path.Combine(dir, name);
            }

            static string MakeKeyForManifest(string path)
            {
                var dir = Path.GetFullPath(Path.GetDirectoryName(path) ?? ".");
                var fileName = Path.GetFileName(path);
                if (fileName.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName[..^".manifest.json".Length];
                }
                return Path.Combine(dir, fileName);
            }

            var nefByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var manifestByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in files)
            {
                if (f.EndsWith(".nef", StringComparison.OrdinalIgnoreCase))
                {
                    nefByKey[MakeKeyForNef(f)] = f;
                }
                else if (f.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    manifestByKey[MakeKeyForManifest(f)] = f;
                }
            }

            var result = new List<ContractEntry>();
            foreach (var kv in nefByKey)
            {
                manifestByKey.TryGetValue(kv.Key, out var manifestPath);
                result.Add(new ContractEntry
                {
                    NefPath = kv.Value,
                    ManifestPath = manifestPath,
                    NefSize = SafeFileSize(kv.Value),
                    ManifestSize = string.IsNullOrEmpty(manifestPath) ? 0 : SafeFileSize(manifestPath)
                });
            }

            foreach (var mkv in manifestByKey)
            {
                if (!nefByKey.ContainsKey(mkv.Key))
                {
                    result.Add(new ContractEntry
                    {
                        NefPath = mkv.Key + ".nef",
                        ManifestPath = mkv.Value,
                        NefSize = 0,
                        ManifestSize = SafeFileSize(mkv.Value)
                    });
                }
            }

            return result;
        }

        private static long SafeFileSize(string path)
        {
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        private static string? Prompt(string text, string caption)
        {
            using var form = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent
            };
            var label = new Label() { Left = 10, Top = 15, Text = text, AutoSize = true };
            var textBox = new TextBox() { Left = 10, Top = 40, Width = 460 };
            var confirmation = new Button() { Text = "OK", Left = 310, Width = 75, Top = 70, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "Cancel", Left = 395, Width = 75, Top = 70, DialogResult = DialogResult.Cancel };
            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(confirmation);
            form.Controls.Add(cancel);
            form.AcceptButton = confirmation;
            form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        private ProtocolSettings LoadProtocolSettingsFromSelection()
        {
            // Determine config file by comboBox_NeoNetowrk selection/text
            string selection = (comboBox_NeoNetowrk?.SelectedItem?.ToString() ?? comboBox_NeoNetowrk?.Text ?? string.Empty).Trim().ToLowerInvariant();

            string relative = @"..\..\..\config.testnet.json"; // default
            if (selection.Contains("main"))
            {
                relative = @"..\..\..\config.mainnet.json";
            }
            else if (selection.Contains("priv"))
            {
                relative = @"..\..\..\config.privnet.json";
            }
            else if (selection.Contains("test"))
            {
                relative = @"..\..\..\config.testnet.json";
            }

            string full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
            return ProtocolSettings.Load(full);
        }

        private async System.Threading.Tasks.Task<(string txHash, string vmState, string contractHash, string? error)> DeployAsync(string rpcAddress, string wif, string nefFilePath, string manifestFilePath)
        {
            KeyPair senderKey = Neo.Network.RPC.Utility.GetKeyPair(wif);

            var protocolSettings = LoadProtocolSettingsFromSelection();

            RpcClient client = new RpcClient(new Uri(rpcAddress), null, null, protocolSettings);

            ContractClient contractClient = new ContractClient(client);

            var nefFile = NefFile.Parse(File.ReadAllBytes(nefFilePath));
            var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));

            // pre-compute the contract hash that will be deployed
            UInt160 sender = Contract.CreateSignatureContract(senderKey.PublicKey).ScriptHash;
            string computedHash;
            try
            {
                var cHash2 = Neo.SmartContract.Helper.GetContractHash(sender, nefFile.CheckSum, manifest.Name);
                computedHash = cHash2.ToString();
            }
            catch
            {
                computedHash = string.Empty;
            }

            // create the deploy transaction
            Transaction transaction = await contractClient.CreateDeployContractTxAsync(nefFile.ToArray(), manifest, senderKey).ConfigureAwait(false);

            // Broadcast the transaction over the NEO network
            await client.SendRawTransactionAsync(transaction).ConfigureAwait(false);

            // Wait until on-chain
            WalletAPI neoAPI = new WalletAPI(client);
            var appLog = await neoAPI.WaitTransactionAsync(transaction).ConfigureAwait(false);

            // Extract vm state and error details using explicit RPC call
            string vmState = appLog?.VMState?.ToString() ?? string.Empty;
            string? error = null;
            try
            {
                var rpcLog = await client.GetApplicationLogAsync(transaction.Hash.ToString()).ConfigureAwait(false);
                if (rpcLog?.Executions != null && rpcLog.Executions.Count > 0)
                {
                    var exec0 = rpcLog.Executions[0];
                    // VMState is enum
                    try { vmState = exec0.VMState.ToString(); } catch { }

                    // Try to read 'exception' field from JSON
                    try
                    {
                        var j = exec0.ToJson();
                        if (j is Neo.Json.JObject jo)
                        {
                            var ej = jo["exception"];
                            if (ej != null)
                            {
                                try { error = ej.AsString(); } catch { error = ej.ToString(); }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return (transaction.Hash.ToString(), vmState, computedHash, error);
        }









        // New overload that accepts rpcAddress explicitly and performs full validation + fee estimation/sign/send
        public async Task<string?> InvokeSC_WithWIF(string rpcAddress, UInt160 contractScriptHash, string methodName, ContractParameter[] parameters, string wif, WitnessScope scope = WitnessScope.CalledByEntry, long extraSystemFee = 0, long extraNetworkFee = 0, CancellationToken cancellationToken = default)
        {
            if (contractScriptHash is null) throw new ArgumentNullException(nameof(contractScriptHash));
            if (string.IsNullOrWhiteSpace(rpcAddress)) throw new ArgumentException("RPC address is required", nameof(rpcAddress));
            if (string.IsNullOrWhiteSpace(wif)) throw new ArgumentException("WIF is required", nameof(wif));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("MethodName is required", nameof(methodName));
            parameters ??= Array.Empty<ContractParameter>();

            var protocolSettings = LoadProtocolSettingsFromSelection();
            var rpcClient = new RpcClient(new Uri(rpcAddress), null, null, protocolSettings);

            KeyPair keyPair = Neo.Network.RPC.Utility.GetKeyPair(wif);

            // Build script
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(contractScriptHash, methodName, parameters);
            byte[] script = sb.ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            // Build signers (same as deploy pattern)
            UInt160 account = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
            var signers = new[]
            {
                new Signer
                {
                    Scopes = scope,
                    Account = account
                }
            };

            // Use TransactionManagerFactory to build, fee-calc and sign
            var tmFactory = new TransactionManagerFactory(rpcClient);
            var txManager = await tmFactory.MakeTransactionAsync(script, signers).ConfigureAwait(false);
            var tx = await txManager.AddSignature(keyPair).SignAsync().ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await rpcClient.SendRawTransactionAsync(tx).ConfigureAwait(false);
            return tx.Hash.ToString();
        }

        // Backward-compatible wrapper that uses the RPC URL from the textbox
        public async Task<string?> InvokeSC_WithWIF(UInt160 contractScriptHash, string methodName, ContractParameter[] parameters, string wif, WitnessScope scope = WitnessScope.CalledByEntry, long extraSystemFee = 0, long extraNetworkFee = 0, CancellationToken cancellationToken = default)
        {
            var rpc = textBox_RPCAddress.Text;
            return await InvokeSC_WithWIF(rpc, contractScriptHash, methodName, parameters, wif, scope, extraSystemFee, extraNetworkFee, cancellationToken).ConfigureAwait(false);
        }

        private async System.Threading.Tasks.Task<(string? state, long gasConsumed)> InvokeDestroyAsync(string rpcAddress, string wif, string contractHash)
        {
            if (string.IsNullOrWhiteSpace(contractHash)) throw new ArgumentException("Contract hash is required", nameof(contractHash));
            var hash = UInt160.Parse(contractHash);

            var txHash = await InvokeSC_WithWIF(rpcAddress, hash, "destroy", Array.Empty<ContractParameter>(), wif, WitnessScope.CalledByEntry).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(txHash))
            {
                return ("SEND_FAILED", 0);
            }

            // Wait for application log to get final state and gas
            var protocolSettings = LoadProtocolSettingsFromSelection();
            using var client = new RpcClient(new Uri(rpcAddress), null, null, protocolSettings);

            RpcApplicationLog? rpcLog = null;
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    rpcLog = await client.GetApplicationLogAsync(txHash).ConfigureAwait(false);
                    if (rpcLog != null) break;
                }
                catch { }
                await Task.Delay(1000).ConfigureAwait(false);
            }

            if (rpcLog?.Executions != null && rpcLog.Executions.Count > 0)
            {
                var exec0 = rpcLog.Executions[0];
                string state = exec0.VMState.ToString();
                long gas = (long)Math.Ceiling(Convert.ToDecimal(exec0.GasConsumed, CultureInfo.InvariantCulture));
                return (state, gas);
            }

            return ("PENDING", 0);
        }

        private async System.Threading.Tasks.Task<(string? state, long gasConsumed)> InvokeUpdateAsync(string rpcAddress, string wif, string contractHash, string nefFilePath, string manifestFilePath, string? dataInput)
        {
            if (string.IsNullOrWhiteSpace(contractHash)) throw new ArgumentException("Contract hash is required", nameof(contractHash));
            if (string.IsNullOrWhiteSpace(nefFilePath) || !File.Exists(nefFilePath)) throw new FileNotFoundException("NEF file not found", nefFilePath);
            if (string.IsNullOrWhiteSpace(manifestFilePath) || !File.Exists(manifestFilePath)) throw new FileNotFoundException("Manifest file not found", manifestFilePath);

            var hash = UInt160.Parse(contractHash);

            // Load files
            var nefBytes = File.ReadAllBytes(nefFilePath);
            var manifestJson = File.ReadAllText(manifestFilePath);

            // Build params for update(nef, manifest, data?) where
            // nef: ByteArray, manifest: String, data: Any (optional)
            var parameters = new List<ContractParameter>
            {
                new ContractParameter { Type = ContractParameterType.ByteArray, Value = nefBytes },
                new ContractParameter { Type = ContractParameterType.String, Value = manifestJson }
            };

            var dataParam = BuildOptionalDataParameter(dataInput);
            if (dataParam != null) parameters.Add(dataParam);

            var txHash = await InvokeSC_WithWIF(rpcAddress, hash, "update", parameters.ToArray(), wif, WitnessScope.CalledByEntry).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(txHash))
            {
                return ("SEND_FAILED", 0);
            }

            // Wait for application log to get final state and gas
            var protocolSettings = LoadProtocolSettingsFromSelection();
            using var client = new RpcClient(new Uri(rpcAddress), null, null, protocolSettings);

            RpcApplicationLog? rpcLog = null;
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    rpcLog = await client.GetApplicationLogAsync(txHash).ConfigureAwait(false);
                    if (rpcLog != null) break;
                }
                catch { }
                await Task.Delay(1000).ConfigureAwait(false);
            }

            if (rpcLog?.Executions != null && rpcLog.Executions.Count > 0)
            {
                var exec0 = rpcLog.Executions[0];
                string state = exec0.VMState.ToString();
                long gas = (long)Math.Ceiling(Convert.ToDecimal(exec0.GasConsumed, CultureInfo.InvariantCulture));
                return (state, gas);
            }

            return ("PENDING", 0);
        }

        private static ContractParameter? BuildOptionalDataParameter(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Try hex -> byte[]
            try
            {
                var hex = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? input.Substring(2) : input;
                if (hex.Length % 2 == 0 && hex.All(Uri.IsHexDigit))
                {
                    var bytes = Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
                    return new ContractParameter { Type = ContractParameterType.ByteArray, Value = bytes };
                }
            }
            catch { }

            // Try integer
            if (long.TryParse(input, out var n))
            {
                return new ContractParameter { Type = ContractParameterType.Integer, Value = n };
            }

            // Fallback string
            return new ContractParameter { Type = ContractParameterType.String, Value = input };
        }

        private static byte[] NefToBytes(NefFile nef)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            nef.Serialize(writer);
            writer.Flush();
            return ms.ToArray();
        }

        private void button_SaveConfig_Click(object sender, EventArgs e)
        {
            // Save current UI state to JSON config file
            using var sfd = new SaveFileDialog
            {
                Title = "Save configuration",
                FileName = "n3cmanconfig.json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = AppContext.BaseDirectory
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                SaveConfigToFile(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_LoadConfig_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Load configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "n3cmanconfig.json",
                InitialDirectory = AppContext.BaseDirectory
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                LoadConfigFromFile(ofd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetDefaultConfigPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "n3cmanconfig.json");
        }

        private void TrySaveDefaultConfigSnapshot()
        {
            try
            {
                SaveConfigToFile(GetDefaultConfigPath());
            }
            catch { }
        }

        private void SaveConfigToFile(string path)
        {
            var cfg = GetCurrentConfigFromUI();
            var json = System.Text.Json.JsonSerializer.Serialize(cfg, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            // maintain default autoload copy
            var defaultPath = GetDefaultConfigPath();
            try
            {
                if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(defaultPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(defaultPath, json);
                }
            }
            catch { }
        }

        private AppConfig GetCurrentConfigFromUI()
        {
            var cfg = new AppConfig
            {
                RpcAddress = textBox_RPCAddress.Text?.Trim() ?? string.Empty,
                Wif = textBox_WIF.Text?.Trim() ?? string.Empty,
                Contracts = new List<ContractConfigEntry>()
            };

            foreach (ListViewItem item in listView_ContractsFilesList.Items)
            {
                if (item.Tag is not ContractEntry entry) continue;
                cfg.Contracts.Add(new ContractConfigEntry
                {
                    NefPath = entry.NefPath,
                    ManifestPath = entry.ManifestPath ?? string.Empty,
                    Selected = item.Checked,
                    LastDeployTx = entry.LastDeployTx ?? string.Empty,
                    ContractHash = entry.ContractHash ?? string.Empty
                });
            }
            return cfg;
        }

        private void LoadConfigFromFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Configuration file not found", path);
            var json = File.ReadAllText(path);
            var cfg = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            ApplyConfigToUI(cfg);
        }

        private void ApplyConfigToUI(AppConfig cfg)
        {
            textBox_RPCAddress.Text = string.IsNullOrWhiteSpace(cfg.RpcAddress) ? textBox_RPCAddress.Text : cfg.RpcAddress;
            textBox_WIF.Text = cfg.Wif ?? string.Empty;

            listView_ContractsFilesList.BeginUpdate();
            listView_ContractsFilesList.Items.Clear();
            if (cfg.Contracts != null)
            {
                foreach (var c in cfg.Contracts)
                {
                    var nef = c.NefPath ?? string.Empty;
                    var manifest = string.IsNullOrWhiteSpace(c.ManifestPath) ? null : c.ManifestPath;
                    var entry = new ContractEntry
                    {
                        NefPath = nef,
                        ManifestPath = manifest,
                        NefSize = SafeFileSize(nef),
                        ManifestSize = string.IsNullOrEmpty(manifest) ? 0 : SafeFileSize(manifest),
                        LastDeployTx = string.IsNullOrWhiteSpace(c.LastDeployTx) ? null : c.LastDeployTx,
                        ContractHash = string.IsNullOrWhiteSpace(c.ContractHash) ? null : c.ContractHash
                    };

                    var nefName = Path.GetFileName(nef);
                    var manifestDisp = string.IsNullOrEmpty(manifest) ? "<not found>" : Path.GetFileName(manifest);
                    var item = new ListViewItem(nefName)
                    {
                        Tag = entry,
                        Checked = c.Selected
                    };
                    item.SubItems.Add(entry.NefSize.ToString());
                    item.SubItems.Add(manifestDisp);
                    item.SubItems.Add(entry.ManifestSize > 0 ? entry.ManifestSize.ToString() : "0");
                    item.SubItems.Add("Ready");
                    var details = string.Empty;
                    if (!string.IsNullOrWhiteSpace(entry.LastDeployTx))
                    {
                        details = $"Tx: {entry.LastDeployTx}";
                    }
                    item.SubItems.Add(details);
                    EnsureSubItemCapacity(item, GetContractHashColumnIndex());
                    item.SubItems[GetContractHashColumnIndex()].Text = entry.ContractHash ?? string.Empty;
                    listView_ContractsFilesList.Items.Add(item);
                }
            }
            listView_ContractsFilesList.EndUpdate();
        }

        private class AppConfig
        {
            public string RpcAddress { get; set; } = string.Empty;
            public string Wif { get; set; } = string.Empty;
            public List<ContractConfigEntry>? Contracts { get; set; }
        }

        private class ContractConfigEntry
        {
            public string NefPath { get; set; } = string.Empty;
            public string? ManifestPath { get; set; }
            public bool Selected { get; set; }
            // New persisted fields
            public string? LastDeployTx { get; set; }
            public string? ContractHash { get; set; }
        }

        private void button_OpenFileFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string? nefPath = null;

                // Prefer selected item; fallback to first checked
                if (listView_ContractsFilesList.SelectedItems.Count > 0)
                {
                    var item = listView_ContractsFilesList.SelectedItems[0];
                    if (item.Tag is ContractEntry entry) nefPath = entry.NefPath;
                }
                else if (listView_ContractsFilesList.CheckedItems.Count > 0)
                {
                    var item = listView_ContractsFilesList.CheckedItems[0];
                    if (item.Tag is ContractEntry entry) nefPath = entry.NefPath;
                }

                if (string.IsNullOrWhiteSpace(nefPath))
                {
                    MessageBox.Show("Select a contract in the list", "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var fullPath = Path.GetFullPath(nefPath);
                if (File.Exists(fullPath))
                {
                    var arg = $"/select, \"{fullPath}\"";
                    System.Diagnostics.Process.Start("explorer.exe", arg);
                    return;
                }

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    var arg = $"\"{dir}\"";
                    System.Diagnostics.Process.Start("explorer.exe", arg);
                    return;
                }

                MessageBox.Show($"File/folder not found: {fullPath}", "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Explorer: {ex.Message}", "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}