namespace LanChat
{
    partial class SelectAdapterForm
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.confrim = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 20);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(276, 20);
            this.comboBox1.TabIndex = 0;
            // 
            // confrim
            // 
            this.confrim.Location = new System.Drawing.Point(213, 55);
            this.confrim.Name = "confrim";
            this.confrim.Size = new System.Drawing.Size(75, 23);
            this.confrim.TabIndex = 1;
            this.confrim.Text = "确定";
            this.confrim.UseVisualStyleBackColor = true;
            this.confrim.Click += new System.EventHandler(this.confrim_Click);
            // 
            // SelectAdapterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(303, 90);
            this.Controls.Add(this.confrim);
            this.Controls.Add(this.comboBox1);
            this.Name = "SelectAdapterForm";
            this.Text = "选择适配器";
            this.Load += new System.EventHandler(this.SelectAdapterForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button confrim;
    }
}