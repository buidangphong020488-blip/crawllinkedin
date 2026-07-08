
namespace crawlData
{
    partial class FrmExtractor
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            lblStatus = new System.Windows.Forms.Label();
            lblInput = new System.Windows.Forms.Label();
            lblOutput = new System.Windows.Forms.Label();
            hopeForm1 = new ReaLTaiizor.Forms.HopeForm();
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
            pnlToolbar = new System.Windows.Forms.Panel();
            hopeForm1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInput).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvOutput).BeginInit();
            pnlToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.Location = new System.Drawing.Point(511, 289);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(37, 15);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "STOP";
            // 
            // lblInput
            // 
            lblInput.AutoSize = true;
            lblInput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblInput.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            lblInput.Location = new System.Drawing.Point(12, 53);
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
            lblOutput.Location = new System.Drawing.Point(12, 293);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(65, 19);
            lblOutput.TabIndex = 5;
            lblOutput.Text = "OUTPUT";
            // 
            // hopeForm1
            // 
            hopeForm1.ControlBoxColorH = System.Drawing.Color.FromArgb(228, 231, 237);
            hopeForm1.ControlBoxColorHC = System.Drawing.Color.FromArgb(245, 108, 108);
            hopeForm1.ControlBoxColorN = System.Drawing.Color.White;
            hopeForm1.Controls.Add(lblPcKey);
            hopeForm1.Dock = System.Windows.Forms.DockStyle.Top;
            hopeForm1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            hopeForm1.ForeColor = System.Drawing.Color.FromArgb(242, 246, 252);
            hopeForm1.Image = null;
            hopeForm1.Location = new System.Drawing.Point(0, 0);
            hopeForm1.MaximizeBox = false;
            hopeForm1.Name = "hopeForm1";
            hopeForm1.Size = new System.Drawing.Size(1385, 40);
            hopeForm1.TabIndex = 7;
            hopeForm1.Text = "W5G Extractor";
            hopeForm1.ThemeColor = System.Drawing.Color.FromArgb(24, 24, 37);
            // 
            // lblPcKey
            // 
            lblPcKey.AutoSize = true;
            lblPcKey.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblPcKey.ForeColor = System.Drawing.Color.FromArgb(166, 227, 161);
            lblPcKey.Location = new System.Drawing.Point(129, 13);
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
            dgvInput.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dgvInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgvInput.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvInput.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(137, 180, 250);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInput.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvInput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvInput.DefaultCellStyle = dataGridViewCellStyle2;
            dgvInput.EnableHeadersVisualStyles = false;
            dgvInput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dgvInput.GridColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dgvInput.Location = new System.Drawing.Point(12, 71);
            dgvInput.Name = "dgvInput";
            dgvInput.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInput.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
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
            dgvOutput.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dgvOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgvOutput.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvOutput.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(137, 180, 250);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvOutput.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dgvOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvOutput.DefaultCellStyle = dataGridViewCellStyle5;
            dgvOutput.EnableHeadersVisualStyles = false;
            dgvOutput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dgvOutput.GridColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dgvOutput.Location = new System.Drawing.Point(12, 337);
            dgvOutput.MultiSelect = false;
            dgvOutput.Name = "dgvOutput";
            dgvOutput.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(69, 71, 90);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvOutput.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
            dgvOutput.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvOutput.RowTemplate.Height = 25;
            dgvOutput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvOutput.Size = new System.Drawing.Size(1361, 415);
            dgvOutput.TabIndex = 11;
            dgvOutput.CellContentClick += dgvOutput_CellContentClick;
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
            btnExport.Location = new System.Drawing.Point(300, 5);
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
            label1.Location = new System.Drawing.Point(700, 12);
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
            lblProxy.Location = new System.Drawing.Point(745, 12);
            lblProxy.Name = "lblProxy";
            lblProxy.Size = new System.Drawing.Size(37, 15);
            lblProxy.TabIndex = 4;
            lblProxy.Text = "None";
            // 
            // flpColumns
            // 
            flpColumns.Location = new System.Drawing.Point(12, 311);
            flpColumns.Name = "flpColumns";
            flpColumns.Size = new System.Drawing.Size(1361, 20);
            flpColumns.TabIndex = 12;
            // 
            // chkUseProxy
            // 
            chkUseProxy.AutoSize = true;
            chkUseProxy.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            chkUseProxy.Location = new System.Drawing.Point(630, 10);
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
            btnTotalData.Location = new System.Drawing.Point(400, 5);
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
            chkRecaptcha.Location = new System.Drawing.Point(520, 10);
            chkRecaptcha.Name = "chkRecaptcha";
            chkRecaptcha.Size = new System.Drawing.Size(103, 19);
            chkRecaptcha.TabIndex = 13;
            chkRecaptcha.Text = "Use Recaptcha";
            chkRecaptcha.UseVisualStyleBackColor = true;
            chkRecaptcha.CheckedChanged += chkRecaptcha_CheckedChanged;
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
            // FrmExtractor
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            ClientSize = new System.Drawing.Size(1385, 766);
            Controls.Add(flpColumns);
            Controls.Add(dgvOutput);
            Controls.Add(dgvInput);
            Controls.Add(lblOutput);
            Controls.Add(lblInput);
            Controls.Add(pnlToolbar);
            Controls.Add(hopeForm1);
            Controls.Add(lblStatus);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(1920, 1040);
            MinimumSize = new System.Drawing.Size(190, 40);
            Name = "FrmExtractor";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "W5G- Goole - LinkedIn Extractor";
            hopeForm1.ResumeLayout(false);
            hopeForm1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInput).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvOutput).EndInit();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblInput;
        private System.Windows.Forms.Label lblOutput;
        private ReaLTaiizor.Forms.HopeForm hopeForm1;
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
        private System.Windows.Forms.Panel pnlToolbar;
    }
}

