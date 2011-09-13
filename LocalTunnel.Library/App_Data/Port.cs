using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data;
using System.IO;

namespace LocalTunnel.Library
{
    /// <summary>
    /// POCO Class to represent a port information
    /// </summary>
    public class Port
    {
        /// <summary>
        /// Id of the port
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Number of time the port has been tunneled
        /// </summary>
        public int UsedTimes { get; set; }

        /// <summary>
        /// Last time used
        /// </summary>
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Service host the user connected to
        /// </summary>
        public string ServiceHost { get; set; }

        #region Methods

        public static void AddUsage(int portNumber, string serviceHost) {
            List<Port> listUsed = GetUsedPorts();
            Port found = null;

            if (listUsed.Count > 0)
            {
                found = listUsed.Where(port => port.Number == portNumber && port.ServiceHost.ToLower().Trim().Equals(serviceHost.ToLower().Trim())).First();
            }

            bool update = true;
            if (found == null)
            {
                update = false;
                found = new Port() { ServiceHost = serviceHost, Number = portNumber, UsedTimes = 0 };
            }

            found.LastUsed = DateTime.Now;
            found.UsedTimes = found.UsedTimes + 1;

            string dbfile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName + "\\App_Data\\Data.sdf";
            SqlCeConnection connection = new SqlCeConnection("datasource=" + dbfile);
            connection.Open();

            SqlCeCommand command = connection.CreateCommand();

            if (update)
            {
                command.CommandText = string.Format("UPDATE Port SET UsedTimes = '{0}', LastUsed = '{1}' WHERE Id = {2}", found.UsedTimes, found.LastUsed, found.Id);
            }
            else
            {
                command.CommandText = string.Format("INSERT INTO Port (Number, ServiceHost, UsedTimes, LastUsed) VALUES ('{0}', '{1}','{2}','{3}')", found.Number, found.ServiceHost, found.UsedTimes, found.LastUsed);            
            }

            command.ExecuteNonQuery();

            connection.Close();
        }

        /// <summary>
        /// Gets the list of used ports. 
        /// </summary>
        /// <returns></returns>
        public static List<Port> GetUsedPorts()
        {
            string dbfile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName + "\\App_Data\\Data.sdf";
            SqlCeConnection connection = new SqlCeConnection("datasource=" + dbfile);

            SqlCeDataAdapter adapter = new SqlCeDataAdapter("SELECT * FROM Port ORDER BY LastUsed", connection);
            DataSet data = new DataSet();
            adapter.Fill(data);
            connection.Close();

            List<Port> list = new List<Port>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                list.Add(new Port()
                {
                    Number = (int)row["Number"],
                    LastUsed = (DateTime)row["LastUsed"],
                    UsedTimes = (int)row["UsedTimes"],
                    ServiceHost = (string)row["ServiceHost"],
                    Id = (int)row["Id"]
                });
            }

            return list;
        }

        #endregion

    }
}
