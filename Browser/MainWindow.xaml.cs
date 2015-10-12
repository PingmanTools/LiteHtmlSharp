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
using System.Diagnostics;

namespace Browser
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      Stopwatch _watch = new Stopwatch();
      HTMLVisual _renderer;
      public MainWindow()
      {
         InitializeComponent();

         _renderer = new HTMLVisual(canvas);
      }

      private void button_Click(object sender, RoutedEventArgs e)
      {
         _watch.Restart();
         string html = @"<html><head></head><body><div style='width:100px; height:100px; background-color:red'>HELLO WORLD</div></body></html>";
         _renderer.Render(html);
         _watch.Stop();
         lbTime.Content = "Render Time(ms): " + _watch.ElapsedMilliseconds;
      }

      private void btClear_Click(object sender, RoutedEventArgs e)
      {
         _renderer.Clear();
         lbTime.Content = string.Empty;
      }
   }
}
