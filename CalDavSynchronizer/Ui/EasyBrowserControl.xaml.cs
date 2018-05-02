using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Office.Interop.Outlook;

namespace CalDavSynchronizer.Ui
{
  /// <summary>
  /// Interaktionslogik für EasyBrowserControl.xaml
  /// </summary>
  public partial class EasyBrowserControl : UserControl
  {
    public EasyBrowserControl()
    {
      InitializeComponent();


      webBrowser.Navigate("http://caldav.easyproject.com/issues/4");
    }
  }
}
