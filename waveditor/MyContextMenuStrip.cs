using System.Windows.Forms;
using System.ComponentModel;

namespace waveditor
{
    class MyContextMenuStrip : ContextMenuStrip
    {
        /*
        public MyContextMenuStrip(IContainer ccc)
            : base(ccc)
        {
        }
         */
        protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
        {
            try
            {
                base.OnItemClicked(e);
            }
            finally
            {
                if (this.AutoClose)
                {
                    this.Close();
                }
            }
        }
    }
}
