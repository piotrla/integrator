using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace kanc_integrator
{
    public class DbRepo
    {
        private const string AppSettingsFile = "appsettings.json";
        private AuthenticationConfig _config { get; set; }
        private AuthRepo _authRepo;
        private bool _isSuccessfullyConfigured = false;
        private int _interval = 0;

        public bool IsSuccessfullyConfigured { get { return _isSuccessfullyConfigured; } }
        public int Interval { get { return _interval; } }
        public DbRepo()
        {
            _config = LoadAppSettings();
            LoadEnvVariables(_config);
        }

        private void LoadEnvVariables(AuthenticationConfig config)
        {
            if (int.TryParse(_config.Interval.ToString(), out int interval))
            {
                _authRepo = new AuthRepo(config);
                _isSuccessfullyConfigured = true;
                _interval = interval;
            }
            else
            {
                System.Console.WriteLine("Interval value must be valid. Only integers allowed.");
            }

        }

        private AuthenticationConfig LoadAppSettings()
        {
            var config = new ConfigurationBuilder()
                                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                .AddJsonFile(AppSettingsFile, optional: false, reloadOnChange: true)
                                .AddUserSecrets<AuthenticationConfig>()
                                .AddEnvironmentVariables()
                                .Build();

            return config.Get<AuthenticationConfig>();
        }

        public async Task<Dictionary<string, string>> GetAccessToken()
        {
            //Get fresh accessToken from MSAL
            Dictionary<string, string> accessToken = await _authRepo.GetAccessTokenAsync();
            return accessToken;
        }


        public async Task<ActionInfoModel<ActionModel>> GetDataFromRemoteResources(string accessToken)
        {
            try
            {
                ActionInfoModel<ActionModel> actionInfo = new ActionInfoModel<ActionModel>();
                List<ActionModel> actions = new List<ActionModel>();

                //Get data from database (current Actions to pick - 1 day past only)
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //Actions
                    using (HttpResponseMessage response = await httpClient.GetAsync(_config.EndPoint + "/daemon/actions"))
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        actions = JsonConvert.DeserializeObject<List<ActionModel>>(content);
                    }
                }

                actionInfo.ListOfElements = actions == null ? new List<ActionModel>() : actions;
                actionInfo.ReturnMessage = new ReturnMessage()
                {
                    Id = Guid.NewGuid(),
                    OperationDate = DateTime.UtcNow + TimeSpan.FromHours(1),
                    OperationMessage = $"Data downloaded correctly. Total actions: {actionInfo.ListOfElements.Count()}"
                };

                return actionInfo;
            }
            catch (Exception ex)
            {
                return new ActionInfoModel<ActionModel>()
                {
                    ListOfElements = new List<ActionModel>(),
                    ReturnMessage = new ReturnMessage()
                    {
                        Id = Guid.NewGuid(),
                        OperationDate = DateTime.UtcNow + TimeSpan.FromHours(1),
                        OperationMessage = $"Exception in {this.GetType().Name}.\r\n{ex.ToString()}"
                    }
                };

            }
        }

        public async Task<Dictionary<bool, string>> UpdateClients(string accessToken)
        {
            try
            {
                Dictionary<bool, string> dic = new Dictionary<bool, string>();

                //Get data from database
                List<ClientModel> clients = await GetClients();
                if (clients.Any())
                {
                    //Update data via api                         
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        //Clients
                        var content = new StringContent(JsonConvert.SerializeObject(clients), Encoding.UTF8, "application/json");
                        using (HttpResponseMessage response = await httpClient.PostAsync(_config.EndPoint + "/daemon/clients", content))
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            dic.Add(true, responseContent.Replace("\"", ""));
                        }
                    }
                }
                else
                {
                    dic.Add(false, "Error, there is no clients to update.");
                }

                return dic;
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string>() { { false, ex.ToString() } };
            }
        }

        private async Task<List<ClientModel>> GetClients()
        {
            MySqlConnection connection = new MySqlConnection(_config.DatabaseConnection);
            try
            {
                string query = "SELECT Id, NazwaKlienta, Nip, Ulica, Miejscowosc, KodPocztowy, Email, Telefon, Notatka, " +
                "Stawka, Pesel, RodzajKlienta, NazwaKlientaPelna FROM t_kontrahent";
                MySqlCommand command1 = new MySqlCommand(query, connection);
                DataTable dt = new DataTable();
                await connection.OpenAsync();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(command1))
                {
                    await adapter.FillAsync(dt);
                }
                List<ClientModel> lista = new List<ClientModel>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new ClientModel()
                    {
                        Id = Convert.ToInt32(row[0].ToString()),
                        NazwaKlienta = row[1].ToString(),
                        Nip = row[2].ToString(),
                        Ulica = row[3].ToString(),
                        Miejscowosc = row[4].ToString(),
                        KodPocztowy = row[5].ToString(),
                        Email = row[6].ToString(),
                        Telefon = row[7].ToString(),
                        Notatka = row[8].ToString(),
                        Stawka = Int32.TryParse(row[9].ToString(), out int stawka) ? stawka : (int?)null,
                        Pesel = row[10].ToString(),
                        RodzajKlienta = row[11].ToString(),
                        NazwaKlientaPelna = row[12].ToString()
                    });
                }

                return lista;
            }
            catch (Exception)
            {
                return new List<ClientModel>();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<Dictionary<bool, string>> UpdateCases(string accessToken)
        {
            try
            {
                Dictionary<bool, string> dic = new Dictionary<bool, string>();

                //Get data from database
                List<CaseModel> currentCases = await GetCases();
                if (currentCases.Any())
                {
                    //Update data via api                         
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        //Clients
                        var content = new StringContent(JsonConvert.SerializeObject(currentCases), Encoding.UTF8, "application/json");
                        using (HttpResponseMessage response = await httpClient.PostAsync(_config.EndPoint + "/daemon/cases", content))
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            dic.Add(true, responseContent.Replace("\"", ""));
                        }
                    }
                }
                else
                {
                    dic.Add(false, "Error, there is no cases to update.");
                }

                return dic;
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string>() { { false, ex.ToString() } };
            }
        }

        private async Task<List<CaseModel>> GetCases()
        {
            MySqlConnection connection = new MySqlConnection(_config.DatabaseConnection);
            try
            {
                DateTime date = DateTime.Now - TimeSpan.FromDays(30);
                string query = "SELECT c.Id, c.KontrahentId, c.NazwaSprawy, c.DataWprowadzeniaSprawy, c.RodzajSprawy, c.Sygnatura, c.NrWewnetrzny, c.Status " +
               "FROM t_sprawa as c WHERE c.Status = 'Otwarty' AND c.DataWprowadzeniaSprawy >= @_dataWprowadzeniaSprawy";

                MySqlCommand command1 = new MySqlCommand(query, connection);
                command1.Parameters.Add(new MySqlParameter("_dataWprowadzeniaSprawy", MySqlDbType.Date) { Value = date.Date });
                DataTable dt = new DataTable();
                await connection.OpenAsync();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(command1))
                {
                    adapter.Fill(dt);
                }
                List<CaseModel> lista = new List<CaseModel>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new CaseModel()
                    {
                        Id = Guid.Parse(row[0].ToString()),
                        KontrahentId = Convert.ToInt32(row[1].ToString()),
                        NazwaSprawy = row[2].ToString(),
                        DataWprowadzeniaSprawy = DateTime.Parse(row[3].ToString()),
                        RodzajSprawy = row[4].ToString(),
                        Sygnatura = row[5].ToString(),
                        NrWewnetrzny = Convert.ToInt32(row[6].ToString()),
                        Status = row[7].ToString()
                    });
                }

                return lista;
            }
            catch (Exception)
            {
                return new List<CaseModel>();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private async Task<List<UserModel>> GetUsers()
        {
            MySqlConnection connection = new MySqlConnection(_config.DatabaseConnection);
            try
            {
                string query = "SELECT Id, NazwaUzytkownika, Imie, Nazwisko, Funkcja, Aktywny, Password, Email FROM t_uzytkownik";
                MySqlCommand command1 = new MySqlCommand(query, connection);
                DataTable dt = new DataTable();
                await connection.OpenAsync();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(command1))
                {
                    await adapter.FillAsync(dt);
                }
                List<UserModel> lista = new List<UserModel>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new UserModel()
                    {
                        Id = Convert.ToInt32(row[0].ToString()),
                        NazwaUzytkownika = row[1].ToString(),
                        Nazwisko = row[3].ToString(),
                        Imie = row[2].ToString(),
                        Funkcja = row[4].ToString(),
                        Aktywny = Convert.ToBoolean(row[5].ToString()),
                        Password = row[6].ToString(),
                        Email = row[7].ToString()
                    });
                }
                return lista;
            }
            catch (Exception)
            {
                return new List<UserModel>();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<string> FeedActions(List<ActionModel> actions)
        {
            MySqlConnection connection = new MySqlConnection(_config.DatabaseConnection);
            try
            {
                await connection.OpenAsync();
                //Get Users and remake identifier
                List<UserModel> users = await GetUsers();
                int newActions = 0;
                foreach (ActionModel action in actions)
                {
                    //query check item exists
                    string query = "SELECT Id FROM t_czynnosc WHERE Id = @_id";
                    MySqlCommand cmdSelect = new MySqlCommand(query, connection);
                    cmdSelect.Parameters.Add(new MySqlParameter("_id", MySqlDbType.Guid) { Value = action.Id });
                    var scalarCheck = await cmdSelect.ExecuteScalarAsync();
                    //if no exists add new
                    if (scalarCheck == null)
                    {
                        string queryInsert = "INSERT INTO t_czynnosc (Id, SprawaId, Wykonawca, DataWykonania, CzasWykonania, Opis, Status, IsEdited, WhoEditing) VALUES " +
                                  "(@_id, @_sprawaId, @_wykonawca, @_dataWykonania, @_czasWykonania, @_opis, @_status, @_isEdited, @_whoEditing)";

                        UserModel user = users.Where(x => x.Email == action.Wykonawca).FirstOrDefault();
                        MySqlCommand command1 = new MySqlCommand(queryInsert, connection);
                        command1.Parameters.Add(new MySqlParameter("_id", MySqlDbType.Guid) { Value = action.Id });
                        command1.Parameters.Add(new MySqlParameter("_sprawaId", MySqlDbType.Guid) { Value = action.SprawaId });
                        command1.Parameters.Add(new MySqlParameter("_wykonawca", MySqlDbType.VarChar) { Value = user != null ? user.NazwaUzytkownika : action.Wykonawca });
                        command1.Parameters.Add(new MySqlParameter("_dataWykonania", MySqlDbType.Date) { Value = action.DataWykonania });
                        command1.Parameters.Add(new MySqlParameter("_czasWykonania", MySqlDbType.Decimal) { Value = action.CzasWykonania });
                        command1.Parameters.Add(new MySqlParameter("_opis", MySqlDbType.Text) { Value = action.Opis });
                        command1.Parameters.Add(new MySqlParameter("_status", MySqlDbType.VarChar) { Value = action.Status });
                        command1.Parameters.Add(new MySqlParameter("_isEdited", MySqlDbType.Int16) { Value = action.IsEdited });
                        command1.Parameters.Add(new MySqlParameter("_whoEditing", MySqlDbType.VarChar) { Value = action.WhoEditing });
                        int numberOfSaved = await command1.ExecuteNonQueryAsync();
                        if (numberOfSaved > 0)
                        {
                            newActions++;
                        }
                    }
                }

                return $"FeedActions. Operation succeed. New actions: {newActions}";
            }
            catch (Exception ex)
            {
                return $"FeedActions. Exception. {ex.ToString()}";
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}