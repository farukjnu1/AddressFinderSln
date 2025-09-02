using AddressFinder.BO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace AddressFinder
{
    public partial class Form1 : Form
    {
        static string baseUri = "http://maps.googleapis.com/maps/api/" +
                          "geocode/xml?latlng={0},{1}&sensor=false";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            /* Write here MSSQL Connection and read Terminal ID Table (example:Table__xxxxxxxxx)
            Record one by one until end of table. make sure every rec need 1sec delay. if lat and lon 
            is same data then go to next rec. i mean only get new lat lon record.*/

            // get from db
            //declaration of Sql connection
            string GpsConStr = ConfigurationManager.ConnectionStrings["GpsDbConStr"].ConnectionString;
            string BaseStationConStr = ConfigurationManager.ConnectionStrings["BaseStationConStr"].ConnectionString;
            SqlConnection con;
            SqlCommand cmd;
            SqlDataReader rd;

            //check for username and password in Employee table whether it exist or not
            con = new SqlConnection(GpsConStr);
            cmd = new SqlCommand();
            cmd.Connection = con;
            con.Open();
            cmd.CommandText = "SELECT DISTINCT dbLon, dbLat FROM " + tbTrack.Text.Trim() + "";
            rd = cmd.ExecuteReader();
            List<Track> tracks = new List<Track>();
            while (rd.Read())
            {
                Track t = new Track();
                t.Lon = rd.GetDecimal(0);
                t.Lat = rd.GetDecimal(1);
                tracks.Add(t);
            }
            rd.Close();
            cmd.Dispose();
            con.Close();

            #region get all from Table_AddressEx
            //con = new SqlConnection(BaseStationConStr);
            //cmd = new SqlCommand();
            //cmd.Connection = con;
            //con.Open();
            //cmd.CommandText = "SELECT [nID],[dbLon],[dbLat],[strAddress] FROM [Table_AddressEx]";
            //rd = cmd.ExecuteReader();
            //List<Table_AddressEx> Table_AddressExS = new List<Table_AddressEx>();
            //while (rd.Read())
            //{
            //    Table_AddressEx Table_AddressEx = new Table_AddressEx();
            //    Table_AddressEx.nID = rd.GetInt32(0);
            //    Table_AddressEx.dbLon = rd.GetDecimal(1);
            //    Table_AddressEx.dbLat = rd.GetDecimal(2);
            //    Table_AddressEx.strAddress = rd.GetString(3);

            //    Table_AddressExS.Add(Table_AddressEx);
            //}
            //rd.Close();
            //con.Close();
            #endregion

            foreach (Track track in tracks)
            {
                //var insertTable_AddressEx = (from o in Table_AddressExS where o.dbLat == track.Lat && o.dbLon == track.Lon select o).FirstOrDefault();
                con = new SqlConnection(BaseStationConStr);
                cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                cmd.CommandText = "SELECT [nID],[dbLon],[dbLat],[strAddress] FROM [Table_AddressEx] WHERE [dbLon] = '" + track.Lon + "' AND [dbLat] = '" + track.Lat + "'";
                rd = cmd.ExecuteReader();
                rd.Read();
                bool Null = rd.HasRows;
                rd.Close();
                cmd.Dispose();
                con.Close();

                if (!Null)
                {
                    string address = GetGeoLocation(track.Lat, track.Lon);
                    if (address != string.Empty)
                    {
                        con = new SqlConnection(BaseStationConStr);
                        cmd = new SqlCommand();
                        cmd.Connection = con;
                        con.Open();
                        cmd.CommandText = "INSERT INTO [dbo].[Table_AddressEx]([dbLon],[dbLat],[strAddress])VALUES('" + track.Lon + "','" + track.Lat + "','" + address.Replace("'", "''") + "')";
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();
                        con.Close();
                    }
                    
                }
            }
            MessageBox.Show("Task complete!");
            // go to next Rec /
        }

        public string GetGeoLocation(decimal Latitude, decimal Longitude)
        {
            // delay for geocode request
            Random random = new Random();
            int randomMs = random.Next(15000, 30000);
            Thread.Sleep(randomMs);

            string myAddress0 = string.Empty;
            string myAddress1 = string.Empty;
            WebClient wc = new WebClient();
            string result = string.Empty;
            try
            {
                result = wc.DownloadString("http://maps.googleapis.com/maps/api/geocode/xml?latlng=" + Latitude + "," + Longitude + "&sensor=false");
            }
            catch
            {
                return string.Empty;
            }
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(result);
            int nodes = xmlDoc.SelectNodes("/GeocodeResponse/result/formatted_address").Count;
            nodes = nodes - 2;

            //Get and display the last item node.
            XmlElement root = xmlDoc.DocumentElement;
            XmlNodeList nodeList;
            nodeList = root.GetElementsByTagName("formatted_address");
            string address;
            string addresses = string.Empty;
            for (int i = 0; i < nodes; i++)
            {
                address = nodeList.Item(i).InnerXml;
                if (i < 1)
                {
                    addresses = address.Split(',')[0];
                }
                else
                {
                    addresses += ", " + address.Split(',')[0];
                }
            }

            if (addresses != string.Empty)
            {
                addresses = "gm: " + addresses;
            }

            return addresses;
        }


        public static void RetrieveFormatedAddress(string lat, string lng)
        {
            string requestUri = string.Format(baseUri, lat, lng);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                wc.DownloadStringAsync(new Uri(requestUri));
            }
        }

        static void wc_DownloadStringCompleted(object sender,
                 DownloadStringCompletedEventArgs e)
        {
            var xmlElm = XElement.Parse(e.Result);

            var status = (from elm in xmlElm.Descendants()
                          where elm.Name == "status"
                          select elm).FirstOrDefault();
            if (status.Value.ToLower() == "ok")
            {
                var res = (from elm in xmlElm.Descendants()
                           where elm.Name == "formatted_address"
                           select elm).FirstOrDefault();
                //   Insert data Lat, Lon, and res.Value to Table_Address. /
                //Console.WriteLine(res.Value);
                MessageBox.Show(res.Value);
            }
            else
            {
                //Console.WriteLine("No Address Found");
                MessageBox.Show("No Address Found");
            }
        }


    }
}
