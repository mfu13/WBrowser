
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace WBrowser
{
    public partial class WBrowser : Form
    {
        public static String favXml = "favorits.xml", linksXml = "links.xml";
        String settingsXml="settings.xml", historyXml="history.xml";
        List<String> urls = new List<String>();
        XmlDocument settings = new XmlDocument();
        String homePage;
        CultureInfo currentCulture;
        List<Point> points = new List<Point>();

        System.IO.StreamWriter file = new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//cor.txt");

        public WBrowser()
        {
            InitializeComponent();
            currentCulture = CultureInfo.CurrentCulture;          
        }

        #region Form load/Closing/Closed

        private void Render(string inputUrl, Rectangle crop)
        {
            WebBrowser wb = new WebBrowser();
            wb.ScrollBarsEnabled = false;
            wb.ScriptErrorsSuppressed = true;
            wb.Navigate(inputUrl);
            while (wb.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
            }
            wb.Width = getCurrentBrowser().Document.Body.ScrollRectangle.Width;
            wb.Height = getCurrentBrowser().Document.Body.ScrollRectangle.Height;
            using (Bitmap bitmap = new Bitmap(wb.Width, wb.Height))
            {
                wb.DrawToBitmap(bitmap, new Rectangle(0, 0, wb.Width, wb.Height));
                wb.Dispose();
                Rectangle rect = new Rectangle(crop.Left, crop.Top, wb.Width - crop.Width - crop.Left, wb.Height - crop.Height - crop.Top);
                Bitmap cropped = bitmap.Clone(rect, bitmap.PixelFormat);
                string outputPath = System.IO.Directory.GetCurrentDirectory();
                try
                {
                    cropped.Save(outputPath + "\\heatmap" + "\\fur.png", System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception)
                {
                   
                }
            }
            
           /* String outputFile = System.IO.Directory.GetCurrentDirectory()
                     + "\\heatmap" + "\\mil.png";*/
            String originalImage = System.IO.Directory.GetCurrentDirectory()
                    + "\\heatmap" + "\\fur.png";
         /*   HeatMap myMap = new HeatMap(points, outputFile, originalImage);
            myMap.createHeatMap(0.3f);*/

            // Prepare the process to run
            ProcessStartInfo start = new ProcessStartInfo();
            // Enter in the command line arguments, everything you would enter after the executable name itself
            //start.Arguments = arguments;
            // Enter the executable to run, including the complete path
            start.FileName = System.IO.Directory.GetCurrentDirectory() + "\\heatmap.exe";
            // Do you want to show a console window?
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;

            // Run the external process & wait for it to finish
            using (Process proc = Process.Start(start))
            {
                proc.WaitForExit();
                int exitCode = proc.ExitCode;
                if (exitCode == 0)
                {
                    MessageBox.Show("Heatmap created.", "WBrowser",
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                else {
                    MessageBox.Show("Heatmap could not created !...", "WBrowser",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void HtmlPageEventHandler(object sender, HtmlElementEventArgs e)
        {
            HtmlDocument document = (HtmlDocument)sender;

            // Scroll position
            Point scrollPos = new Point(document.GetElementsByTagName("HTML")[0].ScrollLeft,
                                        document.GetElementsByTagName("HTML")[0].ScrollTop);

            // Actual mouse position in a page
            Point actualMousePosition = new Point(e.ClientMousePosition.X + scrollPos.X,
                                                  e.ClientMousePosition.Y + scrollPos.Y);

            file.WriteLine(actualMousePosition.X + " " + actualMousePosition.Y);
            Console.WriteLine("Actual Mouse Position: " + actualMousePosition.ToString());
        }
       
//visible items
        private void setVisibility()
        {
            if (!File.Exists(settingsXml))
            {
                XmlElement r = settings.CreateElement("settings");
                settings.AppendChild(r);
                XmlElement el ;
                
                el=settings.CreateElement("menuBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("adrBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("linkBar");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("favoritesPanel");
                el.SetAttribute("visible","True");
                r.AppendChild(el);

                el = settings.CreateElement("SplashScreen");
                el.SetAttribute("checked", "True");
                r.AppendChild(el);

                 el = settings.CreateElement("homepage");
                el.InnerText="http://www.google.com.tr";
                r.AppendChild(el);

                el = settings.CreateElement("dropdown");
                el.InnerText = "15";
                r.AppendChild(el);
            }
            else
            {
                settings.Load(settingsXml);
                XmlElement r = settings.DocumentElement;
                menuBar.Visible = (r.ChildNodes[0].Attributes[0].Value.Equals("True"));
                adrBar.Visible = (r.ChildNodes[1].Attributes[0].Value.Equals("True"));
                //linkBar.Visible=(r.ChildNodes[2].Attributes[0].Value.Equals("True"));
               // favoritesPanel.Visible = (r.ChildNodes[3].Attributes[0].Value.Equals("True"));
                //splashScreenToolStripMenuItem.Checked = (r.ChildNodes[4].Attributes[0].Value.Equals("True"));
                homePage=r.ChildNodes[5].InnerText;
            }

          //  this.linksBarToolStripMenuItem.Checked = linkBar.Visible;
            this.menuBarToolStripMenuItem.Checked = menuBar.Visible;
            this.commandBarToolStripMenuItem.Checked = adrBar.Visible;
            //splashScreenToolStripMenuItem.Checked = (settings.DocumentElement.ChildNodes[4].Attributes[0].Value.Equals("True"));
            homePage = settings.DocumentElement.ChildNodes[5].InnerText;
        }
        // form load
        private void Form1_Load(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Text = "Done";
            setVisibility();
            addNewTab();
        
        }
        //form closing
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (browserTabControl.TabCount != 2)
            {
                DialogResult dlg_res = (new Close()).ShowDialog();

                if (dlg_res == DialogResult.No) { e.Cancel = true; closeTab(); }
                else if (dlg_res == DialogResult.Cancel) e.Cancel = true;
                else Application.ExitThread();
            }
        }
        //form closed
        private void WBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            settings.Save(settingsXml);
            File.Delete("source.txt");
        }

         #endregion

        #region FAVORITES,LINKS,HISTORY METHODS 

        //addLink method
        private void addLink(String url, string name)
        {
            XmlDocument myXml = new XmlDocument();
            XmlElement el = myXml.CreateElement("link");
            el.SetAttribute("url", url);
            el.InnerText = name;

            if (!File.Exists(linksXml))
            {
                XmlElement root = myXml.CreateElement("links");
                myXml.AppendChild(root);
                root.AppendChild(el);
            }
            else
            {
                myXml.Load(linksXml);
                myXml.DocumentElement.AppendChild(el);
            }
            
           
            myXml.Save(linksXml);
        }
       
        //renameLink method
        private void renameLink()
        {
            RenameLink rl = new RenameLink(name);
            if (rl.ShowDialog() == DialogResult.OK)
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(linksXml);
                foreach (XmlElement x in myXml.DocumentElement.ChildNodes)
                {
                    if (x.InnerText.Equals(name))
                    {
                        x.InnerText = rl.newName.Text;
                        break;
                    }
                }
        
            }
            rl.Close();
        }
         
     
//delete history
        private void deleteHistory()
        {
            XmlDocument myXml = new XmlDocument();
            myXml.Load(historyXml);
            XmlElement root = myXml.DocumentElement;
            foreach (XmlElement x in root.ChildNodes)
            {
                if (x.GetAttribute("url").Equals(adress))
                {
                    root.RemoveChild(x);
                    break;
                }
            }
           
            myXml.Save(historyXml);
        }

        #endregion

        #region TABURI
        /*TAB-uri*/

        //addNewTab method
        private void addNewTab()
        {
            TabPage tpage = new TabPage();
            tpage.BorderStyle = BorderStyle.Fixed3D;
            browserTabControl.TabPages.Insert(browserTabControl.TabCount - 1, tpage);
            WebBrowser browser = new WebBrowser();
            browser.Navigate(homePage);   
            tpage.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
            browserTabControl.SelectTab(tpage);
            browser.ProgressChanged += new WebBrowserProgressChangedEventHandler(Form1_ProgressChanged);
            browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(Form1_DocumentCompleted);
            browser.Navigating += new WebBrowserNavigatingEventHandler(Form1_Navigating);
            browser.CanGoBackChanged += new EventHandler(browser_CanGoBackChanged);
            browser.CanGoForwardChanged += new EventHandler(browser_CanGoForwardChanged);
        }

        //DocumentCompleted
        private void Form1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser currentBrowser = getCurrentBrowser();
            this.toolStripStatusLabel1.Text = "Done";
            String text = "Blank Page";

            // Add event handler to current page
            currentBrowser.Document.MouseMove += new HtmlElementEventHandler(HtmlPageEventHandler);

            if (!currentBrowser.Url.ToString().Equals("http://www.google.com.tr"))
            {
                text = currentBrowser.Url.Host.ToString();
            }

            this.adrBarTextBox.Text = currentBrowser.Url.ToString();
            browserTabControl.SelectedTab.Text = text;

            img.Image = favicon(currentBrowser.Url.ToString(), "net.png");

            if (!urls.Contains(currentBrowser.Url.Host.ToString()))
                urls.Add(currentBrowser.Url.Host.ToString());

   
        }
        //ProgressChanged    
        private void Form1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            if (e.CurrentProgress < e.MaximumProgress)
                toolStripProgressBar1.Value=(int)e.CurrentProgress;
            else toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;

        }
        //Navigating
        private void Form1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            this.toolStripStatusLabel1.Text = getCurrentBrowser().StatusText;

        }
        //closeTab method
        private void closeTab()
        {
            if (browserTabControl.TabCount != 2)
            {
                browserTabControl.TabPages.RemoveAt(browserTabControl.SelectedIndex);
            }

        }
        //selected index changed
        private void browserTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (browserTabControl.SelectedIndex == browserTabControl.TabPages.Count - 1) addNewTab();
            else
            {
                if (getCurrentBrowser().Url != null)
                    adrBarTextBox.Text = getCurrentBrowser().Url.ToString();
                else adrBarTextBox.Text = "http://www.google.com.tr";

                if (getCurrentBrowser().CanGoBack) toolStripButton1.Enabled = true;
                else toolStripButton1.Enabled = false;

                if (getCurrentBrowser().CanGoForward) toolStripButton2.Enabled = true;
                else toolStripButton2.Enabled = false;
            }
        }

        /* tab context menu */

        private void closeTabToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            closeTab();
        }
        private void duplicateTabToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                Uri dup_url = getCurrentBrowser().Url;
                addNewTab();
                getCurrentBrowser().Url = dup_url;

            }
            else addNewTab();
        }
        #endregion

        #region FAVICON
       
        // favicon
        public static Image favicon(String u, string file)
        {
                Uri url = new Uri(u);
                String iconurl = "http://" + url.Host + "/favicon.ico";

                WebRequest request = WebRequest.Create(iconurl);
                try
                {
                    WebResponse response = request.GetResponse();

                    Stream s = response.GetResponseStream();
                    return Image.FromStream(s);
                }
                catch (Exception ex)
                {
                    return Image.FromFile(file);
                }
            
           
        }
        //favicon index
        private int faviconIndex(string url)
        {
            Uri key = new Uri(url);
            if (!imgList.Images.ContainsKey(key.Host.ToString()))
                imgList.Images.Add(key.Host.ToString(), favicon(url, "link.png"));
            return imgList.Images.IndexOfKey(key.Host.ToString());
        }
        //getFavicon from key
        private Image getFavicon(string key)
        {
            Uri url = new Uri(key);
            if (!imgList.Images.ContainsKey(url.Host.ToString()))
                imgList.Images.Add(url.Host.ToString(), favicon(key
                    , "link.png"));
            return imgList.Images[url.Host.ToString()];
        }
        #endregion

        #region     TOOL CONTEXT MENU
        /* TOOL CONTEXT MENU*/

        //link bar
       
        //menu bar
        private void menuBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            menuBar.Visible = !menuBar.Visible;
            this.menuBarToolStripMenuItem.Checked = menuBar.Visible;
            settings.DocumentElement.ChildNodes[0].Attributes[0].Value = menuBar.Visible.ToString();
        }
        //address bar
        private void commandBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adrBar.Visible = !adrBar.Visible;
            this.commandBarToolStripMenuItem.Checked = adrBar.Visible;
            settings.DocumentElement.ChildNodes[1].Attributes[0].Value = adrBar.Visible.ToString();
        }
        #endregion

        #region ADDRESS BAR
        /*ADDRESS BAR*/

        private WebBrowser getCurrentBrowser()
        {
            return (WebBrowser)browserTabControl.SelectedTab.Controls[0];
        }
        //ENTER
        private void adrBarTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getCurrentBrowser().Navigate(adrBarTextBox.Text);

            }
        }
        //select all from adr bar
        private void adrBarTextBox_Click(object sender, EventArgs e)
        {
            adrBarTextBox.SelectAll();
        }
        //show urls

        private void showUrl()
        {
            if (File.Exists(historyXml))
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(historyXml);
                int i = 0;
                int num=int.Parse(settings.DocumentElement.ChildNodes[6].InnerText.ToString());
                foreach (XmlElement el in myXml.DocumentElement.ChildNodes)
                {
                    if (num <= i++ ) break;
                    else  adrBarTextBox.Items.Add(el.GetAttribute("url").ToString());
                           
                }
            }
        }

        private void adrBarTextBox_DropDown(object sender, EventArgs e)
        {
            adrBarTextBox.Items.Clear();
            showUrl();
        }
        //navigate on selected url 
        private void adrBarTextBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(adrBarTextBox.SelectedItem.ToString());
        }
     //canGoForwardChanged
        void browser_CanGoForwardChanged(object sender, EventArgs e)
        {
            toolStripButton2.Enabled = !toolStripButton2.Enabled;
        }
        //canGoBackChanged
        void browser_CanGoBackChanged(object sender, EventArgs e)
        {
            toolStripButton1.Enabled = !toolStripButton1.Enabled;
        }
        //back  
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoBack();
        }
        //forward
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoForward();
        }
        //go
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(adrBarTextBox.Text);

        }
        //refresh
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Refresh();
        }
        //stop
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Stop();
        }
      
     

        #endregion

        #region LINKS BAR

        /*LINKS BAR*/

        string adress, name;


        //add to favorits bar button
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
                addLink(getCurrentBrowser().Url.ToString(), getCurrentBrowser().Url.ToString());
        }

        //showLinks on link bar
        private void showLinks()
        {
            if (File.Exists(linksXml))
            {
                XmlDocument myXml = new XmlDocument();
                myXml.Load(linksXml);
                XmlElement root = myXml.DocumentElement;
                foreach (XmlElement el in root.ChildNodes)
                {
                    ToolStripButton b =
                        new ToolStripButton(el.InnerText, getFavicon(el.GetAttribute("url")), items_Click, el.GetAttribute("url"));

                    b.ToolTipText = el.GetAttribute("url");
                    b.MouseUp += new MouseEventHandler(b_MouseUp);
               
                }
            }
        }
        //click link button
        private void items_Click(object sender, EventArgs e)
        {
            ToolStripButton b = (ToolStripButton)sender;
            getCurrentBrowser().Navigate(b.ToolTipText);
        }
        //show context menu on button
        private void b_MouseUp(object sender, MouseEventArgs e)
        {
            ToolStripButton b = (ToolStripButton)sender;
            adress = b.ToolTipText;
            name = b.Text;

            if (e.Button == MouseButtons.Right)
                linkContextMenu.Show(MousePosition);
        }


        #endregion

        #region LINK, FAVORITES, HISTORY CONTEXT MENU
        /*GENERAL*/

        //open
        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(adress);
        }
        //open in new tab
        private void openInNewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addNewTab();
            getCurrentBrowser().Navigate(adress);
        }
        //open in new window
        private void openInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WBrowser new_form = new WBrowser();
            new_form.Show();
            new_form.getCurrentBrowser().Navigate(adress);
        }
                     /*LINK CONTEXT MENU*/
      
        //rename link
        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renameLink();
        }


//delete history
        private void deleteToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            deleteHistory();
        }

        private void renderPage_Click(object sender, EventArgs e)
        {
            Render(getCurrentBrowser().Url.ToString(), new Rectangle(0, 0, 0, 0));
        }
 

        #endregion

        #region FAVORITES WINDOW




        #endregion

        #region FAVORITS
        /*FAVORITES*/


        #endregion

        #region FILE
        /*FILE*/

        //new tab
        private void newTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addNewTab();
        }
        //duplicate tab
        private void duplicateTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (getCurrentBrowser().Url != null)
            {
                Uri dup_url = getCurrentBrowser().Url;
                addNewTab();
                getCurrentBrowser().Url = dup_url;

            }
            else addNewTab();
        }
        //new window
        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new WBrowser()).Show();

        }
        //close tab
        private void closeTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeTab();
        }
        //open
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Open(getCurrentBrowser())).Show();
        }
        //page setup
        private void pageSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPageSetupDialog();
        }
        //save as
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowSaveAsDialog();
        }
        //print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPrintDialog();

        }
        //print preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPrintPreviewDialog();
        }
        //properties
        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().ShowPropertiesDialog();
        }
         #endregion

        #region EDIT
        /*EDIT*/
        //cut
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Cut", false, null);

        }
        //copy
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Copy", false, null);

        }
        //paste
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("Paste", false, null);
        }
        //select all
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Document.ExecCommand("SelectAll", true, null);
        }
        #endregion

        #region VIEW
       

        /*Go to*/
//drop down opening
    
        private void goto_click(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(sender.ToString());
        }
        //back
        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoBack();
        }
        //forward
        private void forwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().GoForward();
        }
        //home
        private void homePageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Navigate(homePage);
        }
                    /*Stop*/
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Stop();
        }
                    /*Refresh*/
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getCurrentBrowser().Refresh();
        }
                     /*view source*/
        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String source=("source.txt");
            StreamWriter writer =File.CreateText(source);
            writer.Write(getCurrentBrowser().DocumentText);
            writer.Close();
            Process.Start("notepad.exe", source);            
        }
        //text size 
        private void textSizeToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string level = e.ClickedItem.ToString();
         //   smallerToolStripMenuItem.Checked = false;
           // smallestToolStripMenuItem.Checked = false;
           // mediumToolStripMenuItem.Checked = false;
           // largerToolStripMenuItem.Checked = false;
           // largestToolStripMenuItem.Checked = false;
            switch (level)
            {
                case "Smallest": getCurrentBrowser().Document.ExecCommand("FontSize", true, "0");
                               //  smallestToolStripMenuItem.Checked = true;
                                 break;
                case "Smaller": getCurrentBrowser().Document.ExecCommand("FontSize", true, "1");
                                 //smallerToolStripMenuItem.Checked = true;
                                 break;
                case "Medium": getCurrentBrowser().Document.ExecCommand("FontSize",true,"2");
                                 //mediumToolStripMenuItem.Checked = true; 
                                break;
                case "Larger": getCurrentBrowser().Document.ExecCommand("FontSize",true,"3");
                                //largerToolStripMenuItem.Checked = true; 
                                break;
                case "Largest": getCurrentBrowser().Document.ExecCommand("FontSize",true,"4");
                               // largestToolStripMenuItem.Checked = true;
                                 break;
            }
        }

        #endregion

        #region TOOLS

//delete browsing history
        private void deleteBrowserHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteBrowsingHistory b = new DeleteBrowsingHistory();
            if (b.ShowDialog() == DialogResult.OK)
            {
                if (b.History.Checked == true)
                {
                    File.Delete(historyXml);
                
                }
                if (b.TempFiles.Checked == true)
                {
                    urls.Clear();
                    while (imgList.Images.Count > 4)
                        imgList.Images.RemoveAt(imgList.Images.Count-1);
                    File.Delete("source.txt");

                }
            }
        }
//internet options
        private void internetOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InternetOption intOp = new InternetOption(getCurrentBrowser().Url.ToString());
            if (intOp.ShowDialog() == DialogResult.OK)
            {
                if (!intOp.homepage.Text.Equals(""))
                {
                    homePage = intOp.homepage.Text;
                    settings.DocumentElement.ChildNodes[5].InnerText = intOp.homepage.Text;
                }
                    if (intOp.deleteHistory.Checked == true)
                {
                    File.Delete(historyXml);
    
                }
                settings.DocumentElement.ChildNodes[6].InnerText = intOp.num.Value.ToString();
                ActiveForm.ForeColor = intOp.forecolor;
                ActiveForm.BackColor = intOp.backcolor;
          
                adrBar.BackColor = intOp.backcolor;
                ActiveForm.Font = intOp.font;
               
                menuBar.Font = intOp.font;
            }
        }

        #endregion

        #region HELP
        //about
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new About(false)).Show();
        }
       private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start("mailto:urasfurkan.13@gmail.com");
        }
 #endregion         
    }
}
