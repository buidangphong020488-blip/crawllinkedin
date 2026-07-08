
namespace crawlData
{
    partial class FrmMain
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
            lblStatus = new System.Windows.Forms.Label();
            lblInput = new System.Windows.Forms.Label();
            lblOutput = new System.Windows.Forms.Label();
            lblFormname = new ReaLTaiizor.Forms.HopeForm();
            lblPcKey = new System.Windows.Forms.Label();
            btnConfig = new ReaLTaiizor.Controls.HopeButton();
            btnUploadExcel = new ReaLTaiizor.Controls.HopeButton();
            btnStart = new ReaLTaiizor.Controls.HopeButton();
            btnStop = new ReaLTaiizor.Controls.HopeButton();
            dgvInput = new ReaLTaiizor.Controls.PoisonDataGridView();
            dgvOutput = new ReaLTaiizor.Controls.PoisonDataGridView();
            btnExport = new ReaLTaiizor.Controls.HopeButton();
            label1 = new System.Windows.Forms.Label();
            lblProxy = new System.Windows.Forms.Label();
            flpColumns = new System.Windows.Forms.FlowLayoutPanel();
            chkUseProxy = new System.Windows.Forms.CheckBox();
            btnTotalData = new ReaLTaiizor.Controls.HopeButton();
            chkRecaptcha = new System.Windows.Forms.CheckBox();
            txt_Thread = new System.Windows.Forms.RichTextBox();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            txt_NumCot = new System.Windows.Forms.RichTextBox();
            txt_NumHang = new System.Windows.Forms.RichTextBox();
            statusPanel = new System.Windows.Forms.Panel();
            pnlToolbar = new System.Windows.Forms.Panel();
            lblFormname.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvOutput).BeginInit();
            statusPanel.SuspendLayout();
            pnlToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.Location = new System.Drawing.Point(8, 4);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(41, 15);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Ready";
            // 
            // lblInput
            // 
            lblInput.AutoSize = true;
            lblInput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblInput.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            lblInput.Location = new System.Drawing.Point(12, 88);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(51, 19);
            lblInput.TabIndex = 5;
            lblInput.Text = "INPUT";
            // 
            // lblOutput
            // 
            lblOutput.AutoSize = true;
            lblOutput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblOutput.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            lblOutput.Location = new System.Drawing.Point(12, 300);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(65, 19);
            lblOutput.TabIndex = 5;
            lblOutput.Text = "OUTPUT";
            // 
            // lblFormname
            // 
            lblFormname.ControlBoxColorH = System.Drawing.Color.FromArgb(228, 231, 237);
            lblFormname.ControlBoxColorHC = System.Drawing.Color.FromArgb(245, 108, 108);
            lblFormname.ControlBoxColorN = System.Drawing.Color.White;
            lblFormname.Controls.Add(lblPcKey);
            lblFormname.Dock = System.Windows.Forms.DockStyle.Top;
            lblFormname.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblFormname.ForeColor = System.Drawing.Color.FromArgb(242, 246, 252);
            lblFormname.Image = null;
            lblFormname.Location = new System.Drawing.Point(0, 0);
            lblFormname.Name = "lblFormname";
            lblFormname.Size = new System.Drawing.Size(1385, 40);
            lblFormname.TabIndex = 7;
            lblFormname.Text = "W5G Extractor V1.5";
            lblFormname.ThemeColor = System.Drawing.Color.FromArgb(24, 24, 37);
            // 
            // lblPcKey
            // 
            lblPcKey.AutoSize = true;
            lblPcKey.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblPcKey.ForeColor = System.Drawing.Color.FromArgb(166, 227, 161);
            lblPcKey.Location = new System.Drawing.Point(205, 13);
            lblPcKey.Name = "lblPcKey";
            lblPcKey.Size = new System.Drawing.Size(28, 15);
            lblPcKey.TabIndex = 4;
            lblPcKey.Text = "Key";
            // 
            // btnConfig
            // 
            btnConfig.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnConfig.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnConfig.Cursor = System.Windows.Forms.Cursors.Hand;
            btnConfig.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnConfig.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnConfig.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnConfig.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnConfig.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnConfig.Location = new System.Drawing.Point(530, 5);
            btnConfig.Name = "btnConfig";
            btnConfig.PrimaryColor = System.Drawing.Color.FromArgb(124, 58, 237);
            btnConfig.Size = new System.Drawing.Size(95, 30);
            btnConfig.SuccessColor = System.Drawing.Color.FromArgb(124, 58, 237);
            btnConfig.TabIndex = 8;
            btnConfig.Text = "◆ CONFIG";
            btnConfig.TextColor = System.Drawing.Color.White;
            btnConfig.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnConfig.Click += btnConfig_Click;
            // 
            // btnUploadExcel
            // 
            btnUploadExcel.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnUploadExcel.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnUploadExcel.Cursor = System.Windows.Forms.Cursors.Hand;
            btnUploadExcel.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnUploadExcel.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnUploadExcel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnUploadExcel.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnUploadExcel.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnUploadExcel.Location = new System.Drawing.Point(10, 5);
            btnUploadExcel.Name = "btnUploadExcel";
            btnUploadExcel.PrimaryColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnUploadExcel.Size = new System.Drawing.Size(105, 30);
            btnUploadExcel.SuccessColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnUploadExcel.TabIndex = 9;
            btnUploadExcel.Text = "▲ UPLOAD";
            btnUploadExcel.TextColor = System.Drawing.Color.White;
            btnUploadExcel.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnUploadExcel.Click += btnUploadExcel_Click;
            // 
            // btnStart
            // 
            btnStart.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnStart.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            btnStart.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnStart.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnStart.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnStart.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnStart.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnStart.Location = new System.Drawing.Point(120, 5);
            btnStart.Name = "btnStart";
            btnStart.PrimaryColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnStart.Size = new System.Drawing.Size(85, 30);
            btnStart.SuccessColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnStart.TabIndex = 8;
            btnStart.Text = "▶ START";
            btnStart.TextColor = System.Drawing.Color.White;
            btnStart.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnStop.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            btnStop.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnStop.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnStop.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnStop.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnStop.Location = new System.Drawing.Point(210, 5);
            btnStop.Name = "btnStop";
            btnStop.PrimaryColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnStop.Size = new System.Drawing.Size(85, 30);
            btnStop.SuccessColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnStop.TabIndex = 8;
            btnStop.Text = "■ STOP";
            btnStop.TextColor = System.Drawing.Color.White;
            btnStop.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnStop.Click += btnStop_Click;
            // 
            // dgvInput
            // 
            dgvInput.AllowUserToAddRows = false;
            dgvInput.AllowUserToDeleteRows = false;
            dgvInput.AllowUserToResizeRows = false;
            dgvInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvInput.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dgvInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgvInput.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvInput.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle19.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle19.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle19.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle19.ForeColor = System.Drawing.Color.FromArgb(137, 180, 250);
            dataGridViewCellStyle19.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle19.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle19.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInput.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle19;
            dgvInput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle20.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dataGridViewCellStyle20.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle20.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle20.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle20.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle20.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvInput.DefaultCellStyle = dataGridViewCellStyle20;
            dgvInput.EnableHeadersVisualStyles = false;
            dgvInput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dgvInput.GridColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dgvInput.Location = new System.Drawing.Point(12, 106);
            dgvInput.Name = "dgvInput";
            dgvInput.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle21.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle21.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle21.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle21.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle21.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle21.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInput.RowHeadersDefaultCellStyle = dataGridViewCellStyle21;
            dgvInput.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvInput.RowTemplate.Height = 25;
            dgvInput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvInput.Size = new System.Drawing.Size(1361, 185);
            dgvInput.TabIndex = 10;
            dgvInput.CellFormatting += gridData_CellFormatting;
            // 
            // dgvOutput
            // 
            dgvOutput.AllowUserToDeleteRows = false;
            dgvOutput.AllowUserToResizeRows = false;
            dgvOutput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvOutput.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dgvOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgvOutput.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvOutput.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle22.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle22.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle22.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle22.ForeColor = System.Drawing.Color.FromArgb(137, 180, 250);
            dataGridViewCellStyle22.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle22.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle22.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvOutput.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle22;
            dgvOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle23.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle23.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dataGridViewCellStyle23.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle23.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle23.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle23.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle23.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvOutput.DefaultCellStyle = dataGridViewCellStyle23;
            dgvOutput.EnableHeadersVisualStyles = false;
            dgvOutput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dgvOutput.GridColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dgvOutput.Location = new System.Drawing.Point(12, 340);
            dgvOutput.MultiSelect = false;
            dgvOutput.Name = "dgvOutput";
            dgvOutput.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle24.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle24.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle24.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle24.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle24.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle24.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle24.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvOutput.RowHeadersDefaultCellStyle = dataGridViewCellStyle24;
            dgvOutput.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvOutput.RowTemplate.Height = 25;
            dgvOutput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvOutput.Size = new System.Drawing.Size(1361, 415);
            dgvOutput.TabIndex = 11;
            dgvOutput.CellContentClick += dgvOutput_CellContentClick;
            dgvOutput.CellFormatting += dgvOutput_CellFormatting;
            // 
            // btnExport
            // 
            btnExport.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnExport.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            btnExport.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnExport.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnExport.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnExport.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnExport.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnExport.Location = new System.Drawing.Point(310, 5);
            btnExport.Name = "btnExport";
            btnExport.PrimaryColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btnExport.Size = new System.Drawing.Size(95, 30);
            btnExport.SuccessColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btnExport.TabIndex = 8;
            btnExport.Text = "▼ EXPORT";
            btnExport.TextColor = System.Drawing.Color.White;
            btnExport.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnExport.Click += btnExport_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            label1.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label1.Location = new System.Drawing.Point(1128, 10);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(42, 15);
            label1.TabIndex = 4;
            label1.Text = "Proxy:";
            // 
            // lblProxy
            // 
            lblProxy.AutoSize = true;
            lblProxy.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblProxy.ForeColor = System.Drawing.Color.FromArgb(249, 226, 175);
            lblProxy.Location = new System.Drawing.Point(1173, 10);
            lblProxy.Name = "lblProxy";
            lblProxy.Size = new System.Drawing.Size(37, 15);
            lblProxy.TabIndex = 4;
            lblProxy.Text = "None";
            // 
            // flpColumns
            // 
            flpColumns.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            flpColumns.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            flpColumns.Location = new System.Drawing.Point(12, 318);
            flpColumns.Name = "flpColumns";
            flpColumns.Size = new System.Drawing.Size(1361, 20);
            flpColumns.TabIndex = 12;
            // 
            // chkUseProxy
            // 
            chkUseProxy.AutoSize = true;
            chkUseProxy.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            chkUseProxy.Location = new System.Drawing.Point(1053, 9);
            chkUseProxy.Name = "chkUseProxy";
            chkUseProxy.Size = new System.Drawing.Size(78, 19);
            chkUseProxy.TabIndex = 13;
            chkUseProxy.Text = "Use Proxy";
            chkUseProxy.UseVisualStyleBackColor = true;
            chkUseProxy.CheckedChanged += chkUseProxy_CheckedChanged;
            // 
            // btnTotalData
            // 
            btnTotalData.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnTotalData.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnTotalData.Cursor = System.Windows.Forms.Cursors.Hand;
            btnTotalData.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnTotalData.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnTotalData.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnTotalData.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnTotalData.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnTotalData.Location = new System.Drawing.Point(410, 5);
            btnTotalData.Name = "btnTotalData";
            btnTotalData.PrimaryColor = System.Drawing.Color.FromArgb(8, 145, 178);
            btnTotalData.Size = new System.Drawing.Size(115, 30);
            btnTotalData.SuccessColor = System.Drawing.Color.FromArgb(8, 145, 178);
            btnTotalData.TabIndex = 8;
            btnTotalData.Text = "≡ DATA";
            btnTotalData.TextColor = System.Drawing.Color.White;
            btnTotalData.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnTotalData.Click += btnTotalData_Click;
            // 
            // chkRecaptcha
            // 
            chkRecaptcha.AutoSize = true;
            chkRecaptcha.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            chkRecaptcha.Location = new System.Drawing.Point(939, 9);
            chkRecaptcha.Name = "chkRecaptcha";
            chkRecaptcha.Size = new System.Drawing.Size(103, 19);
            chkRecaptcha.TabIndex = 13;
            chkRecaptcha.Text = "Use Recaptcha";
            chkRecaptcha.UseVisualStyleBackColor = true;
            chkRecaptcha.CheckedChanged += chkRecaptcha_CheckedChanged;
            // 
            // txt_Thread
            // 
            txt_Thread.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txt_Thread.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txt_Thread.Location = new System.Drawing.Point(884, 8);
            txt_Thread.Name = "txt_Thread";
            txt_Thread.Size = new System.Drawing.Size(42, 20);
            txt_Thread.TabIndex = 14;
            txt_Thread.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            label2.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label2.Location = new System.Drawing.Point(839, 11);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(43, 15);
            label2.TabIndex = 5;
            label2.Text = "Thread";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            label3.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label3.Location = new System.Drawing.Point(632, 11);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(132, 15);
            label3.TabIndex = 5;
            label3.Text = "Chrome W (Cols, Rows)";
            // 
            // txt_NumCot
            // 
            txt_NumCot.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txt_NumCot.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txt_NumCot.Location = new System.Drawing.Point(767, 8);
            txt_NumCot.Name = "txt_NumCot";
            txt_NumCot.Size = new System.Drawing.Size(31, 20);
            txt_NumCot.TabIndex = 14;
            txt_NumCot.Text = "";
            // 
            // txt_NumHang
            // 
            txt_NumHang.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txt_NumHang.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txt_NumHang.Location = new System.Drawing.Point(802, 8);
            txt_NumHang.Name = "txt_NumHang";
            txt_NumHang.Size = new System.Drawing.Size(31, 20);
            txt_NumHang.TabIndex = 14;
            txt_NumHang.Text = "";
            // 
            // statusPanel
            // 
            statusPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            statusPanel.Controls.Add(lblStatus);
            statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            statusPanel.Location = new System.Drawing.Point(0, 765);
            statusPanel.Name = "statusPanel";
            statusPanel.Size = new System.Drawing.Size(1385, 25);
            statusPanel.TabIndex = 14;
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = System.Drawing.Color.FromArgb(24, 24, 37);
            pnlToolbar.Controls.Add(btnUploadExcel);
            pnlToolbar.Controls.Add(btnStart);
            pnlToolbar.Controls.Add(btnStop);
            pnlToolbar.Controls.Add(btnExport);
            pnlToolbar.Controls.Add(btnTotalData);
            pnlToolbar.Controls.Add(btnConfig);
            pnlToolbar.Controls.Add(label3);
            pnlToolbar.Controls.Add(txt_NumHang);
            pnlToolbar.Controls.Add(txt_NumCot);
            pnlToolbar.Controls.Add(label2);
            pnlToolbar.Controls.Add(txt_Thread);
            pnlToolbar.Controls.Add(chkRecaptcha);
            pnlToolbar.Controls.Add(chkUseProxy);
            pnlToolbar.Controls.Add(label1);
            pnlToolbar.Controls.Add(lblProxy);
            pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            pnlToolbar.Location = new System.Drawing.Point(0, 40);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new System.Drawing.Size(1385, 40);
            pnlToolbar.TabIndex = 13;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            ClientSize = new System.Drawing.Size(1385, 790);
            Controls.Add(flpColumns);
            Controls.Add(dgvOutput);
            Controls.Add(dgvInput);
            Controls.Add(lblOutput);
            Controls.Add(lblInput);
            Controls.Add(pnlToolbar);
            Controls.Add(lblFormname);
            Controls.Add(statusPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MinimumSize = new System.Drawing.Size(190, 40);
            Name = "FrmMain";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "W5G- Goole - LinkedIn Extractor";
            lblFormname.ResumeLayout(false);
            lblFormname.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvOutput).EndInit();
            statusPanel.ResumeLayout(false);
            statusPanel.PerformLayout();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblInput;
        private System.Windows.Forms.Label lblOutput;
        private ReaLTaiizor.Forms.HopeForm lblFormname;
        private ReaLTaiizor.Controls.HopeButton btnConfig;
        private ReaLTaiizor.Controls.HopeButton btnUploadExcel;
        private ReaLTaiizor.Controls.HopeButton btnStart;
        private ReaLTaiizor.Controls.HopeButton btnStop;
        private ReaLTaiizor.Controls.PoisonDataGridView dgvInput;
        private ReaLTaiizor.Controls.PoisonDataGridView dgvOutput;
        private ReaLTaiizor.Controls.HopeButton btnExport;
        private System.Windows.Forms.Label lblPcKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblProxy;
        private System.Windows.Forms.FlowLayoutPanel flpColumns;
        private System.Windows.Forms.CheckBox chkUseProxy;
        private ReaLTaiizor.Controls.HopeButton btnTotalData;
        private System.Windows.Forms.CheckBox chkRecaptcha;
        private System.Windows.Forms.RichTextBox txt_Thread;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox txt_NumCot;
        private System.Windows.Forms.RichTextBox txt_NumHang;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Panel pnlToolbar;
    }
}

