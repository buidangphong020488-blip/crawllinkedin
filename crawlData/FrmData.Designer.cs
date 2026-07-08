
namespace crawlData
{
    partial class FrmData
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            hopeForm1 = new ReaLTaiizor.Forms.HopeForm();
            btnSearch = new ReaLTaiizor.Controls.HopeButton();
            dgvOutput = new ReaLTaiizor.Controls.PoisonDataGridView();
            flpColumns = new System.Windows.Forms.FlowLayoutPanel();
            btnClose = new ReaLTaiizor.Controls.HopeButton();
            label1 = new System.Windows.Forms.Label();
            txtCompanyName = new System.Windows.Forms.RichTextBox();
            label2 = new System.Windows.Forms.Label();
            txtIndustry = new System.Windows.Forms.RichTextBox();
            btnExport = new ReaLTaiizor.Controls.HopeButton();
            lblOutput = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            txtPosition = new System.Windows.Forms.RichTextBox();
            btnDeleteData = new ReaLTaiizor.Controls.HopeButton();
            pnlToolbar = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)dgvOutput).BeginInit();
            pnlToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // hopeForm1
            // 
            hopeForm1.ControlBoxColorH = System.Drawing.Color.FromArgb(228, 231, 237);
            hopeForm1.ControlBoxColorHC = System.Drawing.Color.FromArgb(245, 108, 108);
            hopeForm1.ControlBoxColorN = System.Drawing.Color.White;
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
            // btnSearch
            // 
            btnSearch.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnSearch.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            btnSearch.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnSearch.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnSearch.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnSearch.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnSearch.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnSearch.Location = new System.Drawing.Point(10, 5);
            btnSearch.Name = "btnSearch";
            btnSearch.PrimaryColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnSearch.Size = new System.Drawing.Size(95, 30);
            btnSearch.SuccessColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnSearch.TabIndex = 8;
            btnSearch.Text = "▶ SEARCH";
            btnSearch.TextColor = System.Drawing.Color.White;
            btnSearch.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnSearch.Click += btnSearch_Click;
            // 
            // dgvOutput
            // 
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
            dgvOutput.Location = new System.Drawing.Point(12, 205);
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
            dgvOutput.Size = new System.Drawing.Size(1361, 535);
            dgvOutput.TabIndex = 11;
            dgvOutput.CellContentClick += dgvOutput_CellContentClick;
            // 
            // flpColumns
            // 
            flpColumns.BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            flpColumns.Location = new System.Drawing.Point(12, 174);
            flpColumns.Name = "flpColumns";
            flpColumns.Size = new System.Drawing.Size(1361, 20);
            flpColumns.TabIndex = 12;
            // 
            // btnClose
            // 
            btnClose.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnClose.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            btnClose.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnClose.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnClose.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnClose.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnClose.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnClose.Location = new System.Drawing.Point(410, 5);
            btnClose.Name = "btnClose";
            btnClose.PrimaryColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnClose.Size = new System.Drawing.Size(85, 30);
            btnClose.SuccessColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnClose.TabIndex = 8;
            btnClose.Text = "× CLOSE";
            btnClose.TextColor = System.Drawing.Color.White;
            btnClose.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnClose.Click += btnClose_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label1.Location = new System.Drawing.Point(12, 100);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(94, 15);
            label1.TabIndex = 13;
            label1.Text = "Company Name";
            // 
            // txtCompanyName
            // 
            txtCompanyName.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txtCompanyName.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txtCompanyName.Location = new System.Drawing.Point(110, 97);
            txtCompanyName.Name = "txtCompanyName";
            txtCompanyName.Size = new System.Drawing.Size(380, 30);
            txtCompanyName.TabIndex = 14;
            txtCompanyName.Text = "";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label2.Location = new System.Drawing.Point(500, 100);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(50, 15);
            label2.TabIndex = 13;
            label2.Text = "Industry";
            // 
            // txtIndustry
            // 
            txtIndustry.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txtIndustry.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txtIndustry.Location = new System.Drawing.Point(555, 97);
            txtIndustry.Name = "txtIndustry";
            txtIndustry.Size = new System.Drawing.Size(380, 30);
            txtIndustry.TabIndex = 14;
            txtIndustry.Text = "";
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
            btnExport.Location = new System.Drawing.Point(110, 5);
            btnExport.Name = "btnExport";
            btnExport.PrimaryColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btnExport.Size = new System.Drawing.Size(95, 30);
            btnExport.SuccessColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btnExport.TabIndex = 15;
            btnExport.Text = "▼ EXPORT";
            btnExport.TextColor = System.Drawing.Color.White;
            btnExport.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnExport.Click += btnExport_Click;
            // 
            // lblOutput
            // 
            lblOutput.AutoSize = true;
            lblOutput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblOutput.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            lblOutput.Location = new System.Drawing.Point(12, 174);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(65, 19);
            lblOutput.TabIndex = 16;
            lblOutput.Text = "OUTPUT";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = System.Drawing.Color.FromArgb(166, 173, 200);
            label3.Location = new System.Drawing.Point(12, 142);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(50, 15);
            label3.TabIndex = 13;
            label3.Text = "Position";
            // 
            // txtPosition
            // 
            txtPosition.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txtPosition.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txtPosition.Location = new System.Drawing.Point(110, 136);
            txtPosition.Name = "txtPosition";
            txtPosition.Size = new System.Drawing.Size(380, 30);
            txtPosition.TabIndex = 14;
            txtPosition.Text = "";
            // 
            // btnDeleteData
            // 
            btnDeleteData.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnDeleteData.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnDeleteData.Cursor = System.Windows.Forms.Cursors.Hand;
            btnDeleteData.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnDeleteData.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnDeleteData.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnDeleteData.HoverTextColor = System.Drawing.Color.White;
            btnDeleteData.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnDeleteData.Location = new System.Drawing.Point(310, 5);
            btnDeleteData.Name = "btnDeleteData";
            btnDeleteData.PrimaryColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnDeleteData.Size = new System.Drawing.Size(95, 30);
            btnDeleteData.SuccessColor = System.Drawing.Color.FromArgb(220, 53, 69);
            btnDeleteData.TabIndex = 17;
            btnDeleteData.Text = "DEL ALL";
            btnDeleteData.TextColor = System.Drawing.Color.White;
            btnDeleteData.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnDeleteData.Click += btnDeleteData_Click;
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = System.Drawing.Color.FromArgb(24, 24, 37);
            pnlToolbar.Controls.Add(btnSearch);
            pnlToolbar.Controls.Add(btnExport);
            pnlToolbar.Controls.Add(btnDeleteData);
            pnlToolbar.Controls.Add(btnClose);
            pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            pnlToolbar.Location = new System.Drawing.Point(0, 40);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new System.Drawing.Size(1385, 40);
            pnlToolbar.TabIndex = 17;
            // 
            // FrmData
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            ClientSize = new System.Drawing.Size(1385, 766);
            Controls.Add(lblOutput);
            Controls.Add(txtIndustry);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(txtPosition);
            Controls.Add(txtCompanyName);
            Controls.Add(label1);
            Controls.Add(flpColumns);
            Controls.Add(dgvOutput);
            Controls.Add(pnlToolbar);
            Controls.Add(hopeForm1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(1920, 1040);
            MinimumSize = new System.Drawing.Size(190, 40);
            Name = "FrmData";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "W5G- Goole - LinkedIn Extractor";
            ((System.ComponentModel.ISupportInitialize)dgvOutput).EndInit();
            pnlToolbar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ReaLTaiizor.Forms.HopeForm hopeForm1;
        private ReaLTaiizor.Controls.HopeButton btnSearch;
        private ReaLTaiizor.Controls.PoisonDataGridView dgvOutput;
        private System.Windows.Forms.FlowLayoutPanel flpColumns;
        private ReaLTaiizor.Controls.HopeButton btnClose;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox txtCompanyName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox txtIndustry;
        private ReaLTaiizor.Controls.HopeButton btnExport;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox txtPosition;
        private ReaLTaiizor.Controls.HopeButton btnDeleteData;
        private System.Windows.Forms.Panel pnlToolbar;
    }
}

