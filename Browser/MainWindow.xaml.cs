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
      HTMLVisual _htmlVisual;
      public MainWindow()
      {
         InitializeComponent();

         _htmlVisual = new HTMLVisual(canvas);
      }

      private void button_Click(object sender, RoutedEventArgs e)
      {
         if (_htmlVisual.HTMLLoaded)
         {
            Draw();
         }
         else
         {
            Render(@"<html><head></head><body><div style='width:100px; height:100px; background-color:red'>HELLO WORLD</div></body></html>");
         }
      }

      private void btClear_Click(object sender, RoutedEventArgs e)
      {
         _htmlVisual.Clear();
         lbTime.Content = string.Empty;
      }

      private void btLoad_Click(object sender, RoutedEventArgs e)
      {
         Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
         dlg.DefaultExt = "html";
         dlg.Filter = "HTML files |*.html";

         if (dlg.ShowDialog() == true)
         {
            string html = System.IO.File.ReadAllText(dlg.FileName);
            Render(html);
         }
      }

      private void Draw()
      {
         _watch.Restart();
         _htmlVisual.Draw();
         _watch.Stop();
         lbTime.Content = "Render Time(ms): " + _watch.ElapsedMilliseconds;
      }

      private void Render(string html)
      {
         _watch.Restart();
         _htmlVisual.Render(html);
         _watch.Stop();
         lbTime.Content = "Render Time(ms): " + _watch.ElapsedMilliseconds;
      }

      private void canvas_MouseMove(object sender, MouseEventArgs e)
      {
         var pos = e.GetPosition(canvas);
         _htmlVisual.OnMouseMove(pos.X, pos.Y);
      }
   }
}
