namespace NeoContractsManager
{
    partial class NeoContractsManager_Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NeoContractsManager_Main));
            listView_ContractsFilesList = new ListView();
            columnHeader_NefContract = new ColumnHeader();
            columnHeader_NefSize = new ColumnHeader();
            columnHeader_ManifestFile = new ColumnHeader();
            columnHeader_ManifestSize = new ColumnHeader();
            columnHeader_DeployStatus = new ColumnHeader();
            columnHeader_TxAnswer = new ColumnHeader();
            columnHeader_ContractHash = new ColumnHeader();
            button_Deploy = new Button();
            button_RemoveContracts = new Button();
            button_DestroySelected = new Button();
            button_AddContracts = new Button();
            textBox_RPCAddress = new TextBox();
            label_RPCAddress = new Label();
            textBox_WIF = new TextBox();
            label_WalletWif = new Label();
            button_SaveConfig = new Button();
            button_LoadConfig = new Button();
            button_UpdateSelected = new Button();
            button_OpenFileFolder = new Button();
            comboBox_NeoNetowrk = new ComboBox();
            label1_NetworkType = new Label();
            SuspendLayout();
            // 
            // listView_ContractsFilesList
            // 
            listView_ContractsFilesList.CheckBoxes = true;
            listView_ContractsFilesList.Columns.AddRange(new ColumnHeader[] { columnHeader_NefContract, columnHeader_NefSize, columnHeader_ManifestFile, columnHeader_ManifestSize, columnHeader_DeployStatus, columnHeader_TxAnswer, columnHeader_ContractHash });
            listView_ContractsFilesList.FullRowSelect = true;
            listView_ContractsFilesList.GridLines = true;
            listView_ContractsFilesList.Location = new Point(12, 51);
            listView_ContractsFilesList.Name = "listView_ContractsFilesList";
            listView_ContractsFilesList.Size = new Size(1347, 343);
            listView_ContractsFilesList.TabIndex = 0;
            listView_ContractsFilesList.UseCompatibleStateImageBehavior = false;
            listView_ContractsFilesList.SelectedIndexChanged += listView_ContractsFilesList_SelectedIndexChanged;
            // 
            // columnHeader_NefContract
            // 
            columnHeader_NefContract.Text = "Nef Contract";
            // 
            // columnHeader_NefSize
            // 
            columnHeader_NefSize.Text = "Nef Size";
            // 
            // columnHeader_ManifestFile
            // 
            columnHeader_ManifestFile.Text = "Manifest file";
            // 
            // columnHeader_ManifestSize
            // 
            columnHeader_ManifestSize.Text = "Manifest Size";
            // 
            // columnHeader_DeployStatus
            // 
            columnHeader_DeployStatus.Text = "Invoke Status";
            // 
            // columnHeader_TxAnswer
            // 
            columnHeader_TxAnswer.Text = "Tx Answer";
            // 
            // columnHeader_ContractHash
            // 
            columnHeader_ContractHash.Text = "Contract Hash";
            // 
            // button_Deploy
            // 
            button_Deploy.Location = new Point(12, 423);
            button_Deploy.Name = "button_Deploy";
            button_Deploy.Size = new Size(129, 23);
            button_Deploy.TabIndex = 1;
            button_Deploy.Text = "Deploy selected";
            button_Deploy.UseVisualStyleBackColor = true;
            button_Deploy.Click += button_Deploy_Click;
            // 
            // button_RemoveContracts
            // 
            button_RemoveContracts.Location = new Point(147, 12);
            button_RemoveContracts.Name = "button_RemoveContracts";
            button_RemoveContracts.Size = new Size(138, 23);
            button_RemoveContracts.TabIndex = 2;
            button_RemoveContracts.Text = "Remove selected files";
            button_RemoveContracts.UseVisualStyleBackColor = true;
            button_RemoveContracts.Click += button_RemoveContracts_Click;
            // 
            // button_DestroySelected
            // 
            button_DestroySelected.Location = new Point(147, 423);
            button_DestroySelected.Name = "button_DestroySelected";
            button_DestroySelected.Size = new Size(138, 23);
            button_DestroySelected.TabIndex = 3;
            button_DestroySelected.Text = "Destroy Selected";
            button_DestroySelected.UseVisualStyleBackColor = true;
            button_DestroySelected.Click += button_DestroySelected_Click;
            // 
            // button_AddContracts
            // 
            button_AddContracts.Location = new Point(12, 12);
            button_AddContracts.Name = "button_AddContracts";
            button_AddContracts.Size = new Size(129, 23);
            button_AddContracts.TabIndex = 4;
            button_AddContracts.Text = "Add Contracts files";
            button_AddContracts.UseVisualStyleBackColor = true;
            button_AddContracts.Click += button_AddContracts_Click;
            // 
            // textBox_RPCAddress
            // 
            textBox_RPCAddress.Location = new Point(669, 423);
            textBox_RPCAddress.Name = "textBox_RPCAddress";
            textBox_RPCAddress.Size = new Size(283, 23);
            textBox_RPCAddress.TabIndex = 5;
            // 
            // label_RPCAddress
            // 
            label_RPCAddress.AutoSize = true;
            label_RPCAddress.Location = new Point(669, 405);
            label_RPCAddress.Name = "label_RPCAddress";
            label_RPCAddress.Size = new Size(74, 15);
            label_RPCAddress.TabIndex = 6;
            label_RPCAddress.Text = "RPC Address";
            // 
            // textBox_WIF
            // 
            textBox_WIF.Location = new Point(958, 423);
            textBox_WIF.Name = "textBox_WIF";
            textBox_WIF.Size = new Size(401, 23);
            textBox_WIF.TabIndex = 7;
            // 
            // label_WalletWif
            // 
            label_WalletWif.AutoSize = true;
            label_WalletWif.Location = new Point(958, 405);
            label_WalletWif.Name = "label_WalletWif";
            label_WalletWif.Size = new Size(27, 15);
            label_WalletWif.TabIndex = 8;
            label_WalletWif.Text = "WIF";
            // 
            // button_SaveConfig
            // 
            button_SaveConfig.Location = new Point(291, 12);
            button_SaveConfig.Name = "button_SaveConfig";
            button_SaveConfig.Size = new Size(108, 23);
            button_SaveConfig.TabIndex = 9;
            button_SaveConfig.Text = "Save Config";
            button_SaveConfig.UseVisualStyleBackColor = true;
            button_SaveConfig.Click += button_SaveConfig_Click;
            // 
            // button_LoadConfig
            // 
            button_LoadConfig.Location = new Point(405, 12);
            button_LoadConfig.Name = "button_LoadConfig";
            button_LoadConfig.Size = new Size(108, 23);
            button_LoadConfig.TabIndex = 10;
            button_LoadConfig.Text = "Load Config";
            button_LoadConfig.UseVisualStyleBackColor = true;
            button_LoadConfig.Click += button_LoadConfig_Click;
            // 
            // button_UpdateSelected
            // 
            button_UpdateSelected.Location = new Point(291, 423);
            button_UpdateSelected.Name = "button_UpdateSelected";
            button_UpdateSelected.Size = new Size(129, 23);
            button_UpdateSelected.TabIndex = 11;
            button_UpdateSelected.Text = "Update Selected";
            button_UpdateSelected.UseVisualStyleBackColor = true;
            button_UpdateSelected.Click += button_UpdateSelected_Click;
            // 
            // button_OpenFileFolder
            // 
            button_OpenFileFolder.Location = new Point(519, 12);
            button_OpenFileFolder.Name = "button_OpenFileFolder";
            button_OpenFileFolder.Size = new Size(131, 23);
            button_OpenFileFolder.TabIndex = 12;
            button_OpenFileFolder.Text = "Open File Folder";
            button_OpenFileFolder.UseVisualStyleBackColor = true;
            button_OpenFileFolder.Click += button_OpenFileFolder_Click;
            // 
            // comboBox_NeoNetowrk
            // 
            comboBox_NeoNetowrk.FormattingEnabled = true;
            comboBox_NeoNetowrk.Location = new Point(439, 423);
            comboBox_NeoNetowrk.Name = "comboBox_NeoNetowrk";
            comboBox_NeoNetowrk.Size = new Size(121, 23);
            comboBox_NeoNetowrk.TabIndex = 13;
            // 
            // label1_NetworkType
            // 
            label1_NetworkType.AutoSize = true;
            label1_NetworkType.Location = new Point(439, 405);
            label1_NetworkType.Name = "label1_NetworkType";
            label1_NetworkType.Size = new Size(52, 15);
            label1_NetworkType.TabIndex = 14;
            label1_NetworkType.Text = "Network";
            // 
            // NeoContractsManager_Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1371, 461);
            Controls.Add(label1_NetworkType);
            Controls.Add(comboBox_NeoNetowrk);
            Controls.Add(button_OpenFileFolder);
            Controls.Add(button_UpdateSelected);
            Controls.Add(button_LoadConfig);
            Controls.Add(button_SaveConfig);
            Controls.Add(label_WalletWif);
            Controls.Add(textBox_WIF);
            Controls.Add(label_RPCAddress);
            Controls.Add(textBox_RPCAddress);
            Controls.Add(button_AddContracts);
            Controls.Add(button_DestroySelected);
            Controls.Add(button_RemoveContracts);
            Controls.Add(button_Deploy);
            Controls.Add(listView_ContractsFilesList);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "NeoContractsManager_Main";
            Text = "Neo Contracts Manager";
            Load += NeoContractsManager_Main_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listView_ContractsFilesList;
        private ColumnHeader columnHeader_NefContract;
        private ColumnHeader columnHeader_NefSize;
        private ColumnHeader columnHeader_ManifestFile;
        private ColumnHeader columnHeader_ManifestSize;
        private ColumnHeader columnHeader_DeployStatus;
        private ColumnHeader columnHeader_TxAnswer;
        private Button button_Deploy;
        private Button button_RemoveContracts;
        private Button button_DestroySelected;
        private Button button_AddContracts;
        private TextBox textBox_RPCAddress;
        private Label label_RPCAddress;
        private TextBox textBox_WIF;
        private Label label_WalletWif;
        private Button button_SaveConfig;
        private Button button_LoadConfig;
        private Button button_UpdateSelected;
        private Button button_OpenFileFolder;
        private ColumnHeader columnHeader_ContractHash;
        private ComboBox comboBox_NeoNetowrk;
        private Label label1_NetworkType;
    }
}
