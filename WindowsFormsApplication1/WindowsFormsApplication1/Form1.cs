using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Fiddler;
using System.Threading;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

        }
        

        static Proxy oSecureEndpoint;
        static string sSecureEndpointHostname = "localhost";
        static int iSecureEndpointPort = 7777;

        private void Form1_Load(object sender, EventArgs e)
        {


            Thread th = new Thread(fidstart );
            th.Start( );

           //heckBox1.Checked = true;
        }











        private void button1_Click(object sender, EventArgs e)
        {
  
            string fidd = Fiddler.CertMaker.trustRootCert().ToString();
            button1.Text = fidd;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            fidexit();



        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)


                fidstart();





            else
                fidexit();

        }

            public void fidstart()
            {

                List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();

                  //设置别名  
            Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp");

            //启动方式  
            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;

            //定义http代理端口  
            int iPort = 8877;
            //启动代理程序，开始监听http请求  
            //端口,是否使用windows系统代理（如果为true，系统所有的http访问都会使用该代理）  
            //Fiddler.FiddlerApplication.Startup(iPort, true, false, true);
            Fiddler.FiddlerApplication.Startup(iPort, oFCSF);
            // 我们还将创建一个HTTPS监听器，当FiddlerCore被伪装成HTTPS服务器有用  
            // 而不是作为一个正常的CERN样式代理服务器。  
            oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);

            textBox1.AppendText("Fiddler Started\n");

            string fidd = Fiddler.CertMaker.trustRootCert().ToString();
            button1.Text = fidd;




            Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
            {
                // Console.WriteLine("Before request for:\t" + oS.fullUrl);
                // In order to enable response tampering, buffering mode MUST
                // be enabled; this allows FiddlerCore to permit modification of
                // the response in the BeforeResponse handler rather than streaming
                // the response to the client as the response comes in.

                Monitor.Enter(oAllSessions);
                oAllSessions.Add(oS);
                Monitor.Exit(oAllSessions);
                oS["X-AutoAuth"] = "(default)";
                oS.bBufferResponse = true;
                /* If the request is going to our secure endpoint, we'll echo back the response.
                
                Note: This BeforeRequest is getting called for both our main proxy tunnel AND our secure endpoint, 
                so we have to look at which Fiddler port the client connected to (pipeClient.LocalPort) to determine whether this request 
                was sent to secure endpoint, or was merely sent to the main proxy tunnel (e.g. a CONNECT) in order to *reach* the secure endpoint.

                As a result of this, if you run the demo and visit https://localhost:7777 in your browser, you'll see

                Session list contains...
                 
                    1 CONNECT http://localhost:7777
                    200                                         <-- CONNECT tunnel sent to the main proxy tunnel, port 8877

                    2 GET https://localhost:7777/
                    200 text/html                               <-- GET request decrypted on the main proxy tunnel, port 8877

                    3 GET https://localhost:7777/               
                    200 text/html                               <-- GET request received by the secure endpoint, port 7777
                */

                if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
                {
                    oS.utilCreateResponseAndBypassServer();
                    oS.oResponse.headers.SetStatus(200, "Ok");
                    oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                    oS.oResponse["Cache-Control"] = "private, max-age=0";
                    oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                }
            };

            /*
                // The following event allows you to examine every response buffer read by Fiddler. Note that this isn't useful for the vast majority of
                // applications because the raw buffer is nearly useless; it's not decompressed, it includes both headers and body bytes, etc.
                //
                // This event is only useful for a handful of applications which need access to a raw, unprocessed byte-stream
                Fiddler.FiddlerApplication.OnReadResponseBuffer += new EventHandler<RawReadEventArgs>(FiddlerApplication_OnReadResponseBuffer);
            */


            Fiddler.FiddlerApplication.BeforeResponse += delegate(Fiddler.Session oS)
            {
                //  Console.WriteLine("{0}:HTTP {1} for {2}", oS.id, oS.responseCode, oS.fullUrl);

                if (oS.uriContains("baidu.com") && oS.oResponse.headers.ExistsAndContains("Content-Type", "text/html"))
                {
                    //获得responsebody，util修改的是全部内容，相当于正则替换所有的内容
                    oS.utilDecodeResponse();
                    oS.utilReplaceInResponse("百度一下","22222222");

                    //  Console.Write("ddddddddddddddddd0         "+x.ToString ()+" dsdjsjdsds           "+y.ToString ());

                    //  oS.utilReplaceInResponse ("百度一下","2222222");

                    textBox1.AppendText("进入百度了了了了了了了了了了了     " + oS.fullUrl+"\n");
                    //textBox1.AppendText("xxxxxxxxxxx\n");
                    //  oS.utilDecodeResponse();
                    // oS.utilReplaceInResponse("百度一下","222222222222");
                    //string text = oS.GetResponseBodyAsString();
                    //  text=text.Replace("百度一下", "222222222222");
                    //  oS.utilSetResponseBody(text);
                    //


                    //  oS.utilSetResponseBody (text  );
                    //  File.WriteAllText(@"d:\1.txt",text  );

                    // oS.SaveResponseBody(Environment.CurrentDirectory + "\\Captcha.txt");








                }








                // Uncomment the following two statements to decompress/unchunk the
                // HTTP response and subsequently modify any HTTP responses to replace 
                // instances of the word "Microsoft" with "Bayden". You MUST also
                // set bBufferResponse = true inside the beforeREQUEST method above.
                //
                //oS.utilDecodeResponse(); oS.utilReplaceInResponse("Microsoft", "Bayden");
            };

          
          
            }

        public  void fidexit()
        {
            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
            Fiddler.FiddlerApplication.Shutdown();
            Thread.Sleep(500);
            button2.Text = "Closed";
            textBox1.AppendText("Fiddler Closed");
            checkBox1.Checked = false;


        }

     

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            fidexit();
            Thread.Sleep(500);

        }
        }


    }

