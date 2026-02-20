// File: FormList.Designer.cs
namespace CommunityEventsApp
{
    partial class FormList
    {
        private System.Windows.Forms.DataGridView dataGridView1;

        private void InitializeComponent()
        {
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            // ... other designer-generated initialization code ...

            // keep event hookup but do NOT define another FormList_Load method here
            this.Load += new System.EventHandler(this.FormList_Load);

            // ... remaining designer code ...
        }

        // NOTE: remove any duplicate FormList_Load method from this file.
    }
}
