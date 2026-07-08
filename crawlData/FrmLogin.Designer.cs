
namespace crawlData
{
    partial class FrmLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtKey = new ReaLTaiizor.Controls.HopeTextBox();
            btnCopy = new ReaLTaiizor.Controls.HopeButton();
            materialLabel1 = new System.Windows.Forms.Label();
            btnClose = new ReaLTaiizor.Controls.HopeButton();
            btnLogin = new ReaLTaiizor.Controls.HopeButton();
            hopeForm1 = new ReaLTaiizor.Forms.HopeForm();
            materialLabel2 = new System.Windows.Forms.Label();
            btn_update = new ReaLTaiizor.Controls.HopeButton();
            SuspendLayout();
            // 
            // txtKey
            // 
            txtKey.BackColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txtKey.BaseColor = System.Drawing.Color.FromArgb(49, 50, 68);
            txtKey.BorderColorA = System.Drawing.Color.FromArgb(137, 180, 250);
            txtKey.BorderColorB = System.Drawing.Color.FromArgb(69, 71, 90);
            txtKey.Enabled = false;
            txtKey.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            txtKey.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            txtKey.Hint = "";
            txtKey.Location = new System.Drawing.Point(48, 99);
            txtKey.MaxLength = 32767;
            txtKey.Multiline = false;
            txtKey.Name = "txtKey";
            txtKey.PasswordChar = '\0';
            txtKey.ScrollBars = System.Windows.Forms.ScrollBars.None;
            txtKey.SelectedText = "";
            txtKey.SelectionLength = 0;
            txtKey.SelectionStart = 0;
            txtKey.Size = new System.Drawing.Size(386, 38);
            txtKey.TabIndex = 0;
            txtKey.TabStop = false;
            txtKey.UseSystemPasswordChar = false;
            // 
            // btnCopy
            // 
            btnCopy.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnCopy.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnCopy.Cursor = System.Windows.Forms.Cursors.Hand;
            btnCopy.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnCopy.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnCopy.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnCopy.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnCopy.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnCopy.Location = new System.Drawing.Point(440, 99);
            btnCopy.Name = "btnCopy";
            btnCopy.PrimaryColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnCopy.Size = new System.Drawing.Size(57, 40);
            btnCopy.SuccessColor = System.Drawing.Color.FromArgb(5, 150, 105);
            btnCopy.TabIndex = 1;
            btnCopy.Text = "COPY";
            btnCopy.TextColor = System.Drawing.Color.White;
            btnCopy.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnCopy.Click += btnCopy_Click;
            // 
            // materialLabel1
            // 
            materialLabel1.AutoSize = true;
            materialLabel1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            materialLabel1.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            materialLabel1.BackColor = System.Drawing.Color.Transparent;
            materialLabel1.Location = new System.Drawing.Point(10, 108);
            materialLabel1.Name = "materialLabel1";
            materialLabel1.Size = new System.Drawing.Size(35, 19);
            materialLabel1.TabIndex = 6;
            materialLabel1.Text = "Key";
            // 
            // btnClose
            // 
            btnClose.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnClose.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            btnClose.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnClose.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnClose.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnClose.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnClose.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnClose.Location = new System.Drawing.Point(308, 164);
            btnClose.Name = "btnClose";
            btnClose.PrimaryColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnClose.Size = new System.Drawing.Size(71, 40);
            btnClose.SuccessColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnClose.TabIndex = 1;
            btnClose.Text = "CLOSE";
            btnClose.TextColor = System.Drawing.Color.White;
            btnClose.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnClose.Click += btnClose_Click;
            // 
            // btnLogin
            // 
            btnLogin.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btnLogin.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            btnLogin.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btnLogin.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btnLogin.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnLogin.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btnLogin.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btnLogin.Location = new System.Drawing.Point(223, 164);
            btnLogin.Name = "btnLogin";
            btnLogin.PrimaryColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnLogin.Size = new System.Drawing.Size(79, 40);
            btnLogin.SuccessColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnLogin.TabIndex = 1;
            btnLogin.Text = "LOGIN";
            btnLogin.TextColor = System.Drawing.Color.White;
            btnLogin.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btnLogin.Click += btnLogin_Click;
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
            hopeForm1.Size = new System.Drawing.Size(509, 40);
            hopeForm1.TabIndex = 7;
            hopeForm1.Text = "W5G Extractor";
            hopeForm1.ThemeColor = System.Drawing.Color.FromArgb(24, 24, 37);
            // 
            // materialLabel2
            // 
            materialLabel2.AutoSize = true;
            materialLabel2.BackColor = System.Drawing.Color.Transparent;
            materialLabel2.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            materialLabel2.ForeColor = System.Drawing.Color.White;
            materialLabel2.Location = new System.Drawing.Point(203, 55);
            materialLabel2.Name = "materialLabel2";
            materialLabel2.Size = new System.Drawing.Size(130, 25);
            materialLabel2.TabIndex = 6;
            materialLabel2.Text = "ĐĂNG NHẬP";
            // 
            // btn_update
            // 
            btn_update.BorderColor = System.Drawing.Color.FromArgb(220, 223, 230);
            btn_update.ButtonType = ReaLTaiizor.Util.HopeButtonType.Primary;
            btn_update.Cursor = System.Windows.Forms.Cursors.Hand;
            btn_update.DangerColor = System.Drawing.Color.FromArgb(245, 108, 108);
            btn_update.DefaultColor = System.Drawing.Color.FromArgb(255, 255, 255);
            btn_update.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btn_update.HoverTextColor = System.Drawing.Color.FromArgb(48, 49, 51);
            btn_update.InfoColor = System.Drawing.Color.FromArgb(144, 147, 153);
            btn_update.Location = new System.Drawing.Point(138, 164);
            btn_update.Name = "btn_update";
            btn_update.PrimaryColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btn_update.Size = new System.Drawing.Size(79, 40);
            btn_update.SuccessColor = System.Drawing.Color.FromArgb(217, 119, 6);
            btn_update.TabIndex = 1;
            btn_update.Text = "UPDATE";
            btn_update.TextColor = System.Drawing.Color.White;
            btn_update.WarningColor = System.Drawing.Color.FromArgb(230, 162, 60);
            btn_update.Click += btn_update_Click;
            // 
            // FrmLogin
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 46);
            ClientSize = new System.Drawing.Size(509, 216);
            Controls.Add(hopeForm1);
            Controls.Add(materialLabel2);
            Controls.Add(materialLabel1);
            Controls.Add(btn_update);
            Controls.Add(btnLogin);
            Controls.Add(btnClose);
            Controls.Add(btnCopy);
            Controls.Add(txtKey);
            ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(1920, 1040);
            MinimumSize = new System.Drawing.Size(190, 40);
            Name = "FrmLogin";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "formTheme1";
            TransparencyKey = System.Drawing.Color.Fuchsia;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ReaLTaiizor.Controls.HopeTextBox txtKey;
        private ReaLTaiizor.Controls.HopeButton btnCopy;
        private System.Windows.Forms.Label materialLabel1;
        private ReaLTaiizor.Controls.HopeButton btnClose;
        private ReaLTaiizor.Controls.HopeButton btnLogin;
        private ReaLTaiizor.Forms.HopeForm hopeForm1;
        private System.Windows.Forms.Label materialLabel2;
        private ReaLTaiizor.Controls.HopeButton btn_update;
    }
}