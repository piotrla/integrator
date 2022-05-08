using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace kanc_integrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"{GetCurrentTime()}: Started..");
            DbRepo _dbRepo = new DbRepo();

            if (_dbRepo.IsSuccessfullyConfigured)
            {
                while (true)
                {
                    //Get fresh accessToken from MSAL
                    Dictionary<string, string> accessToken = await _dbRepo.GetAccessToken();
                    if (accessToken.First().Key == "Success")
                    {
                        string token = accessToken.First().Value;
                        ActionInfoModel<ActionModel> actionsInfo = await _dbRepo.GetDataFromRemoteResources(token);
                        if (actionsInfo.ListOfElements.Any())
                        {
                            Console.WriteLine($"{actionsInfo.ReturnMessage.OperationDate.ToString("yyyy-MM-dd HH:mm:ss")}: {actionsInfo.ReturnMessage.OperationMessage}");
                            string updateInfo = await _dbRepo.FeedActions(actionsInfo.ListOfElements);
                            Console.WriteLine($"{GetCurrentTime()}: " + updateInfo);
                        }
                        else
                        {
                            Console.WriteLine($"{actionsInfo.ReturnMessage.OperationDate.ToString("yyyy-MM-dd HH:mm:ss")}: {actionsInfo.ReturnMessage.OperationMessage}");
                        }

                        Dictionary<bool, string> clientsUpdateInfo = await _dbRepo.UpdateClients(token);
                        Console.WriteLine($"{GetCurrentTime()}: {clientsUpdateInfo.First().Value}");

                        Dictionary<bool, string> casesUpdateInfo = await _dbRepo.UpdateCases(token);
                        Console.WriteLine($"{GetCurrentTime()}: {casesUpdateInfo.First().Value}");
                    }
                    else
                    {
                        Console.WriteLine($"{GetCurrentTime()}: Problem with accessToken. {accessToken.First().Value}");
                    }

                    Console.WriteLine($"{GetCurrentTime()}: Break time: {_dbRepo.Interval}ms [{DecimalToString(_dbRepo.Interval)}min]");
                    await Task.Delay(_dbRepo.Interval);
                    Console.WriteLine($"{GetCurrentTime()}: Next run.");
                }
            }
            else
            {
                System.Console.WriteLine("Break. Configuration has missing variables.");
            }
        }

        private static string DecimalToString(int value)
        {
            return ((decimal)value / 60000).ToString("#.##");
        }

        private static string GetCurrentTime()
        {
            return (DateTime.UtcNow + TimeSpan.FromHours(1)).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
