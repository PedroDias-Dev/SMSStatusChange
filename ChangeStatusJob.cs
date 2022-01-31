using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;

namespace SMSStatusChange
{
    public class ChangeStatusJob : IJob
    {
        private static readonly HttpClient client = new HttpClient();

        public class Mensagem
        {
            public string status { get; set; }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            SqlConnection sqlConn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings.Get("ConnectionString"));

            sqlConn.Open();

            SqlCommand cmd = new SqlCommand("SELECT * FROM MS003 WHERE STAENVIO ='R' AND DATENVIO >='2021-12-15'", sqlConn);

            //  tabela de código de status
            SqlCommand cmdStatus = new SqlCommand("SELECT * FROM MS029", sqlConn);
            //  tabela de tokens de autenticação
            SqlCommand cmdToken = new SqlCommand("SELECT CODPWDCONFIG FROM SG028 WHERE IDCONFIGAPL = 50", sqlConn);
            var apiToken = cmdToken.ExecuteScalar();

            SqlDataReader drStatus = cmdStatus.ExecuteReader();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            while (drStatus.Read())
            {
                try
                {
                    dict.Add(drStatus["DCRSTATUSENVIO"].ToString(), drStatus["CODSTATUSENVIO"].ToString());
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                }
            }

            drStatus.Close();

            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                Console.Out.WriteLine(dr["IDSMS"] + " - " + dr["STAENVIO"]);

                try
                {
                    //Console.WriteLine(apiToken.ToString());
                    var responseString = await client.GetStringAsync("https://v2.bestuse.com.br/api/v1/envioApi/getStatus?token=" + apiToken.ToString() + "&id=" + dr["IDSMS"]);

                    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Mensagem>(responseString);
                    Console.Out.WriteLine(jsonResponse.status);

                    //SqlCommand updateCodeCmd = new SqlCommand();

                    if (jsonResponse.status == "NAO ENTREGUE")
                    {
                        //updateCodeCmd = new SqlCommand("UPDATE MS003 SET CODSTATUSENVIO = " + dict["NÃO ENTREGUE"] + " WHERE Id = " + dr["IDSMS"], sqlConn);
                        Console.Out.WriteLine(dict["NÃO ENTREGUE"] + " " + DateTime.Now);
                    }
                    else if (jsonResponse.status == null)
                    {
                        Console.Out.WriteLine("NULL" + " " + DateTime.Now);
                        throw new Exception(jsonResponse.status);
                    }
                    else
                    {
                        //updateCodeCmd = new SqlCommand("UPDATE MS003 SET CODSTATUSENVIO = " + dict[jsonResponse.status] + " WHERE Id = " + dr["IDSMS"], sqlConn);
                        Console.Out.WriteLine(dict[jsonResponse.status] + " " + DateTime.Now);
                    }

                    //updateCodeCmd.BeginExecuteNonQuery();

                    // alterar campo de tentativas ===> UPDATE MS003 SET QTDTENTATIVA = QTDTENTATIVA + 1 WHERE Id = + dr["IDSMS"]
                    //SqlCommand updateQtyCmd = new SqlCommand("UPDATE MS003 SET QTDTENTATIVA = QTDTENTATIVA + 1 WHERE Id = " + dr["IDSMS"], sqlConn);
                    //updateQtyCmd.BeginExecuteNonQuery();

                    // alterar campo de data verificação
                    //SqlCommand updateDateCmd = new SqlCommand("UPDATE MS003 SET DATVERIFICAO = " + DateTime.Now + " WHERE Id = " + dr["IDSMS"], sqlConn);
                    //updateDateCmd.BeginExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.ToString());
                }

            }

            sqlConn.Close();
        }
    }
}
