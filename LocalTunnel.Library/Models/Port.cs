using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using SQLite;

namespace LocalTunnel.Library.Models
{
    /// <summary>
    /// POCO Class to represent Port Information
    /// </summary>
    public class Port
    {
        #region Properties

        /// <summary>
        /// Id of the port
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Port number
        /// </summary>
        [Indexed]
        public int Number { get; set; }

        /// <summary>
        /// Hostname of the port.
        /// </summary>
        [Indexed]
        public string Host { get; set; }

        /// <summary>
        /// Number of time the port has been tunneled
        /// </summary>
        [Indexed]
        public int UsedTimes { get; set; }

        /// <summary>
        /// Last time used
        /// </summary>
        [Indexed]
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Service host the user connected to
        /// </summary>
        [Indexed]
        public string ServiceHost { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a +1 to the number of times the ort has been used.
        /// For statistic purposes.
        /// </summary>
        /// <param name="localHost"></param>
        /// <param name="portNumber"></param>
        /// <param name="serviceHost"></param>
        public static void AddUsage(string localHost, int portNumber, string serviceHost) {
            //List<Port> listUsed = GetUsedPorts();
            //Port found = null;

            //if (listUsed.Count > 0)
            //{
            //    found = listUsed.Where(port => port.Number == portNumber && port.Host == localHost && port.ServiceHost.ToLower().Trim().Equals(serviceHost.ToLower().Trim())).FirstOrDefault();
            //}

            //bool update = true;
            //if (found == null)
            //{
            //    update = false;
            //    found = new Port() { ServiceHost = serviceHost, Host = localHost, Number = portNumber, UsedTimes = 0 };
            //}

            //found.LastUsed = DateTime.Now;
            //found.UsedTimes = found.UsedTimes + 1;

            //using (SQLiteConnection connection = GetConnection())
            //{
            //    if (update)
            //    {
            //        connection.Update(found);
            //    }
            //    else
            //    {
            //        connection.Insert(found);
            //    }
            //}

        }

        /// <summary>
        /// Gets the SQLite connection.
        /// </summary>
        /// <returns></returns>
        private static SQLiteConnection GetConnection()
        {
            string dirName = string.Format("{0}\\App_Data", new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName);
            string dataFile = string.Format("{0}\\data.db", dirName);
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            if (!File.Exists(dataFile))
            {
                File.Create(dataFile).Close();
            }

            SQLiteConnection db = new SQLiteConnection(dataFile);
            db.CreateTable<Port>();
            
            return db;
        }

        /// <summary>
        /// Gets the list of used ports. 
        /// </summary>
        /// <returns></returns>
        public static List<Port> GetUsedPorts()
        {
            return new List<Port>();
            //using (SQLiteConnection connection = GetConnection())
            //{
            //    return (from p in connection.Table<Port>()
            //            orderby p.LastUsed descending
            //            select p).ToList();
            //}
        }

        #endregion

    }
}
