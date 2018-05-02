using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CalDavSynchronizer.Ui;
using Microsoft.Office.Tools;
using Microsoft.Office.Interop.Outlook;

namespace CalDavSynchronizer.Implementation.ComWrappers
{
  public class InspectorWrapper
  {
    private Microsoft.Office.Interop.Outlook.Inspector inspector;
    private CustomTaskPane taskPane;

    public InspectorWrapper(Inspector Inspector)
    {
      inspector = Inspector;
      ((InspectorEvents_Event)inspector).Close +=
        new InspectorEvents_CloseEventHandler(InspectorWrapper_Close);

      var control = new EasyCustomTaskPaneUserControl();
      var width = control.Width;
      taskPane = Globals.ThisAddIn.CustomTaskPanes.Add (control, "Easy Task Pane", inspector);
      taskPane.VisibleChanged += new EventHandler (TaskPane_VisibleChanged);
      taskPane.Width = inspector.Width/2;
      taskPane.Control.Dock = DockStyle.Fill;
      taskPane.Control.AutoSize = true;
      taskPane.Control.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      //taskPane.Control.Width = 1200;

      //taskPane.Control.Invalidate();

    }

    void TaskPane_VisibleChanged(object sender, EventArgs e)
    {
      Globals.Ribbons[inspector].ManageTaskPaneRibbon.toggleButton1.Checked =
        taskPane.Visible;
    }

    void InspectorWrapper_Close()
    {
      if (taskPane != null)
      {
        Globals.ThisAddIn.CustomTaskPanes.Remove(taskPane);
      }

      taskPane = null;
      Globals.ThisAddIn.InspectorWrappers.Remove(inspector);
      ((InspectorEvents_Event) inspector).Close -=
        new InspectorEvents_CloseEventHandler(InspectorWrapper_Close);
      inspector = null;
    }

    public CustomTaskPane CustomTaskPane
      {
        get
        {
          return taskPane;
        }
  }
  }
}
