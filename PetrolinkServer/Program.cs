using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PetrolinkServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //setting IP address
            IPAddress ip = Dns.GetHostEntry("localhost").AddressList[0];
            //setting TCP listener for the IP
            TcpListener serverSocket = new TcpListener(IPAddress.Any, 7633);
            TcpClient clientSocket = default(TcpClient);
            serverSocket.Start();
            Console.WriteLine(" >> Server Started");
            clientSocket = serverSocket.AcceptTcpClient();
            Console.WriteLine(" >> Accept connection from client");
            //read data from .csv file to DataTable
            DataTable csvData = GetDataTableFromCSVFile(@"../../bin/Debug/MyCurves.csv");

            while ((true))
            {
                //start network stream
                NetworkStream networkStream = clientSocket.GetStream();
                string serverResponse = string.Empty;
                try
                {                                       
                    byte[] bytesFrom = new byte[1500];
                    networkStream.Read(bytesFrom, 0, bytesFrom.Length);
                    string dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                    if (dataFromClient.Contains("$"))
                    {
                        dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

                        if (dataFromClient == "CurveHeader")
                        {
                            serverResponse = GetCurveHeader(csvData);
                        }
                        else
                        {
                            serverResponse = GetCurvePoints(csvData, dataFromClient);
                        }

                        Console.WriteLine(" >> Data from client - " + dataFromClient);
                        
                        Byte[] sendBytes = Encoding.ASCII.GetBytes(serverResponse);
                        networkStream.Write(sendBytes, 0, sendBytes.Length);

                        networkStream.Flush();
                        Console.WriteLine(" >> " + serverResponse);
                        if (dataFromClient != "Stop" && dataFromClient != "CurveHeader" && dataFromClient != "Index")
                        {
                            //to delay the response by 1 second
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }
                    }
                    else
                    {
                        //when client stop the current connection, initialize the client socket connection
                        networkStream.Close();
                        clientSocket = serverSocket.AcceptTcpClient();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());                                                                   
                    networkStream.Close();
                    clientSocket = serverSocket.AcceptTcpClient();
                }
            }
        }



        /// <summary>
        /// To get curve values
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="curve"></param>
        /// <returns></returns>
        private static string GetCurvePoints(DataTable dt, string curve)
        {
            List<string> myCurveData = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                myCurveData.Add(dr[curve].ToString());
            }
            return string.Join(",", myCurveData);
        }

        /// <summary>
        /// To get curve header
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static string GetCurveHeader(DataTable dt)
        {
            List<string> myCurveHead = new List<string>();
            foreach (DataColumn column in dt.Columns)
            {
                myCurveHead.Add(column.ColumnName);
            }
            return string.Join(",", myCurveHead);
        }

        /// <summary>
        /// To read data from CSV file
        /// </summary>
        /// <param name="csv_file_path"></param>
        /// <returns></returns>
        private static DataTable GetDataTableFromCSVFile(string csv_file_path)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }

                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();
                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }
                        csvData.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                //to do exception log
            }
            return csvData;
        }
    }
}
