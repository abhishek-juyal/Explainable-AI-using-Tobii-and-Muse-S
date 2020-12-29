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
using Tobii.Interaction;
using Tobii.Interaction.Wpf;
using System.Drawing;
using System.IO;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using RestSharp;
using System.Text.Json;


namespace HeatTracker
{

    public partial class MainWindow : Window
    {
        Graphics G;
        Bitmap bitmap;
        DateTime start;
        private Host _host;
        private int Xoffset = 0;
        private int Yoffset = 0;
        public int width, height;
        List<Muse> muses;
        Boolean useOffSet = false;
        List<int> X;
        List<int> Y;
        public Boolean close = false;
        public int[,] coordinatesArray;
        int maxCircleRad = 26, count = 1;
        private WpfInteractorAgent _wpfInteractorAgent;
        static string url = "http://127.0.0.1:5000/getData";
        string selectedFileName,path = "C:\\Users\\abhis\\Downloads\\HeatTracker\\BrainTumourProject\\";

        class Muse
        { 
            public List<float> beta;
            public List<float> theta;
            public List<float> alpha;
            public int x;
            public int y;

            public Muse(List<float> bt, List<float> al, List<float> th, int xx , int yy)
            {
                beta = bt;
                alpha = al;
                theta = th;
                x = xx;
                y = yy;
            }
            public void setX(int xx)
            {
                x = xx;
            }

            public void setY(int yy)
            {
                y = yy;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public void set_host(Host ht)
        {
            this._host = ht;
        }
        public void set_wpfInteractorAgent(WpfInteractorAgent wia)
        {
            this._wpfInteractorAgent = wia;
        }

        /**
         * This method call the tobi eye API to get the stream of X and Y, also calls the Muse-S API in python to get
         * the muse s time series.
         */
        protected void getCoordinates()
        {
            muses = new List<Muse>();
            X = new List<int>();
            Y = new List<int>();
            /**
             * code for async call
             * getAsyncData();
             */
            Xoffset = Xoffset / 5;
            Yoffset = Yoffset / 5;
            var gazePointDataStream = _host.Streams.CreateGazePointDataStream();
            // Because timestamp of fixation events is relative to the previous ones only, we will store them in this variable.
            gazePointDataStream.GazePoint((gzx, gzy, ts) =>
            {
                // On the Next event, data comes as FixationData objects, wrapped in a StreamData<T> object.
                SolidBrush maxCircleBr;
                int x = (int)gzx - 20;
                int y = (int)gzy - 50;
                if (useOffSet)
                {
                    x = (int)gzx - Xoffset;
                    y = (int)gzy - Yoffset;
                    int x2 = (int)gzx + Xoffset;
                    int y2 = (int)gzy + Yoffset;
                    x = (x + x2) / 2;
                    y = (y + y2) / 2;
                }
                // change x and y so that it is according to image
                if (x < 0){
                    x = 0;
                }
                if (y<0)
                {
                    y = 0;
                }
                if (x < width && y < height && !close)
                {
                    int count= 0;
                    count = coordinatesArray[x, y];
                    count++;
                    coordinatesArray[x, y] = count;
                    if (count >= 1 && count < 2)
                    {
                        maxCircleBr = new SolidBrush(System.Drawing.Color.YellowGreen);
                    }
                    else if (count >= 2 && count < 3)
                    {
                        maxCircleBr = new SolidBrush(System.Drawing.Color.YellowGreen);
                    }
                    else if (count >=3 && count < 4)
                    {
                        maxCircleBr = new SolidBrush(System.Drawing.Color.YellowGreen);
                    }
                    else if (count >= 4)
                    {
                        maxCircleBr = new SolidBrush(System.Drawing.Color.YellowGreen);
                    }
                    else
                    {
                        maxCircleBr = new SolidBrush(System.Drawing.Color.YellowGreen);
                    }
                    /**
                    * call the muse API to get data
                    */
                    Console.WriteLine("X-- "+x+" Y-- "+y);
                    Boolean callMuse = true;
                    int k = 3+x;
                    int l = x-3;
                    for (int i = 0; i < X.Count; i++)
                    {
                        if(!(X[i] > k || X[i] < l))
                        {
                            callMuse = false;
                        }
                    }
                    if (callMuse)
                    {
                        X.Add(x);
                        Y.Add(y);
                        IRestResponse response = getSyncData();
                        Muse muse = JsonConvert.DeserializeObject<Muse>(response.Content);
                        muse.setX(x);
                        muse.setY(y);
                        muses.Add(muse);
                        /**
                         * fill ellipse
                         */
                        G = Graphics.FromImage(bitmap);
                        G.FillEllipse(maxCircleBr, x, y, maxCircleRad, maxCircleRad);
                        X.Add(x);
                        X.Add(y);
                    }
                }
            });
        }

        /**
         * 
         * this method call the getData API in the python code
         */
        public IRestResponse getSyncData()
        {
            var client = new RestClient(url);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            return response;
        }

        /**
         * 
         * this method call the start API in the python code
         */
        public static async Task getAsyncData()
        {
            await Task.Run(() =>
            {
                // call the python code to run and start collecting the muse-s data
                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                client.Execute(request);
            });
        }

        private void createFinalImage()
        {
            close = true;
            buttonPanel.Visibility = Visibility.Visible;
            if (MessageBox.Show("Do you want to close this window?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                /*
                 * Convert the image to bitmap and download
                 */
                WindowStyle = WindowStyle.ToolWindow;
                BitmapImage tempbi = ToBitmapImage(bitmap);
                ImageControl.Source = tempbi;
                /**
                 * conver the list of muses into json and write in a file
                 */
                string JSONresult = JsonConvert.SerializeObject(muses);
                using (var tw = new StreamWriter(path + "resultJson\\" + selectedFileName + ".json", true))
                {
                    tw.WriteLine(JSONresult.ToString());
                    tw.Close();
                }
                Console.WriteLine("------------x-------------x-----------");
            }
            else
            {
                close = false;
            }
        }

        /**
         * 
         * This method is called when the used pushes the enter key and is done with process, this creates the images with
         * dotted points from muse S and also write to json/csv file
         */
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            DateTime end = DateTime.Now;
            var result = end.Subtract(start).TotalMinutes;
            if (e.Key == Key.Enter && result > 2.7)
            {
                createFinalImage();
            }
            else if (e.Key == Key.Space)
            {
                textBox.Visibility = Visibility.Hidden;
                // create 5 circles on the window
                circle1.Visibility = Visibility.Visible;
                circle2.Visibility = Visibility.Visible;
                circle3.Visibility = Visibility.Visible;
                circle4.Visibility = Visibility.Visible;
                circle5.Visibility = Visibility.Visible;
                circle1.MouseLeftButtonDown += calibrate;
                circle2.MouseLeftButtonDown += calibrate;
                circle3.MouseLeftButtonDown += calibrate;
                circle4.MouseLeftButtonDown += calibrate;
                circle5.MouseLeftButtonDown += calibrate;
            }
            else if(e.Key == Key.L)
            {
                ImageControl.Visibility = Visibility.Hidden;
                buttonPanel.Visibility = Visibility.Visible;
            }
        }

        /**
         * 
         * This method is used to calibrate at the start of the screen when 5 circular dots are displayed
         */
        protected void calibrate(object sender, MouseButtonEventArgs e)
        {
            // get x and y
            int x = 0, y = 0;
            Ellipse elp = e.Source as Ellipse;
            int orgX = (int)Canvas.GetLeft(elp); ;
            int orgY = (int)Canvas.GetTop(elp); ;
            Host caliHost = new Host();
            WpfInteractorAgent caliAgent = caliHost.InitializeWpfAgent();
            var gazePointDataStream = caliHost.Streams.CreateGazePointDataStream();
            gazePointDataStream.GazePoint((gazePointX, gazePointY, timeStamp) =>
            {
                x = (int)gazePointX;
                y = (int)gazePointY;
            });
                Thread.Sleep(100);
                Xoffset = Xoffset + x - orgX;
                Yoffset = Yoffset + y - orgY;
                elp.Visibility = Visibility.Hidden;
                if (count == 5)
                {
                    button.Visibility = Visibility.Visible;
                    TextBlock.Visibility = Visibility.Visible;
                    Console.WriteLine("Final X Offset - " + Xoffset / 5);
                    Console.WriteLine("Final Y Offset - " + Yoffset / 5);
                }
                caliHost.Dispose();
                count++;
        }

        /**
         * 
         * This method is used to load the image when the browse button is clicked
         */
        public void Browse_Folder(object sender, RoutedEventArgs e)
        {
            start = DateTime.Now;
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.InitialDirectory = "C:\\Users\\abhis\\Downloads\\HeatTracker\\result";
            dlg.Filter = "Image files (.jpg)|*.jpg|All Files (.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                close = false;
                selectedFileName = dlg.SafeFileName;
                TextBlock.Text = dlg.FileName;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(dlg.FileName);
                bitmapImage.EndInit();
                bitmap = BitmapImage2Bitmap(bitmapImage);
                ImageControl.Height = bitmap.Height;
                ImageControl.Width = bitmap.Width;
                ImageControl.Source = bitmapImage;
                //System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                //img.Height = bitmapImage.Height;
                //img.Width = bitmapImage.Width;
                //img.Source = bitmapImage;
                //canvasArea.Children.Add(img);
                buttonPanel.Visibility = Visibility.Hidden;
                // bitmap height and weight
                Console.WriteLine("BitImg Width: " + bitmapImage.Width + ", Height: " + bitmapImage.Height);
                Console.WriteLine("BitMap Width: "  + bitmap.Width + "-" + bitmap.Height);
                height = (int)bitmap.Height;
                width = (int)bitmap.Width;
                coordinatesArray = new int[width, height];
                // call to function
                ImageControl.Visibility = Visibility.Visible;
                getCoordinates();
            }
        }

        /**
         * 
         * This method converts bitmap image to bitmap
         */
        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);
                return new Bitmap(bitmap);
            }
        }

        /**
         * 
         * This method generated the image from bitmap and save to desktop
         */
        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            var fileName = path + "resultFolder\\" + selectedFileName;
            (bitmap).Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            //image.StreamSource = ms;
            image.UriSource = new Uri(fileName, UriKind.RelativeOrAbsolute);
            image.EndInit();
            return image;
        }

    }
}
