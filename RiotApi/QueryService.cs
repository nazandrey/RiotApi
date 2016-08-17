using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RiotApi
{
    public static class QueryService
    {
        private static readonly string RIOT_API_KEY = ConfigurationManager.AppSettings["RiotApiKey"];
        private static readonly string RIOT_API_SERVER = ConfigurationManager.AppSettings["RiotApiServer"];

        private static async Task<JObject> httpQuery(string query, string[] argList)
        {
            using (var client = new HttpClient())
            {
                string response;
                try
                {
                    response = await client.GetStringAsync(RIOT_API_SERVER + String.Format(query, argList) + "?api_key=" + RIOT_API_KEY);
                }
                catch (HttpRequestException)
                {

                    throw;
                }

                JObject responseObj = JObject.Parse(response);
                return responseObj;
            }
        }

        public static async Task<Dictionary<string, string>> getLeague(string region, string summonerIdListStr)
        {
            Dictionary<string, string> leagueList = new Dictionary<string, string>();
            await httpQuery("/api/lol/{0}/v2.5/league/by-summoner/{1}/entry", new string[] { region, summonerIdListStr }).ContinueWith(task => {
                List<string> summonerIdList = summonerIdListStr.Split(',').ToList<string>();

                summonerIdList.ForEach(summonerId => {
                    JToken leagueInfo = task.Result?[summonerId]?.First();
                    string tier = "";
                    string division = "";

                    if (leagueInfo != null)
                    {
                        tier = leagueInfo["tier"].ToString();
                        division = leagueInfo["entries"]?.Where(summonerLeagueInfo => summonerLeagueInfo["playerOrTeamId"].ToString() == summonerId).First()["division"].ToString();
                    }
                    else
                    {
                        tier = "UNRANKED";
                    }
                    leagueList.Add(summonerId, tier + (division != "" ? (" " + division) : ""));
                });
            });
            return leagueList;
        }

        public static async Task<Dictionary<string, string>> getSummonerIdByName(string region, string summonerNameListStr)
        {
            Dictionary<string, string> summonerIdList = new Dictionary<string, string>();
            await httpQuery("/api/lol/{0}/v1.4/summoner/by-name/{1}", new string[] { region, summonerNameListStr }).ContinueWith(task => {
                List<string> summonerNameList = summonerNameListStr.Split(',').ToList<string>();

                summonerNameList.ForEach(summonerName => {
                    string summonerResponseName = summonerName.ToLower().Replace(" ", "");
                    var summonerInfo = task.Result[summonerResponseName];
                    var summonerId = summonerInfo["id"];
                    string summonerIdStr = summonerId.ToString();
                    summonerIdList.Add(summonerName, task.Result[summonerResponseName]["id"].ToString());
                });
            });
            return summonerIdList;
        }

        public static async Task<List<JToken>> getSummonerMatchList(string region, string summonerId, int beginIndex, int endIndex)
        {
            List<JToken> summonerMatchList = new List<JToken>();
            await httpQuery("/api/lol/{0}/v2.2/matchlist/by-summoner/{1}?beginIndex={2}&endIndex={3}", new string[] { region, summonerId, beginIndex.ToString(), endIndex.ToString() }).ContinueWith(task => {
                summonerMatchList = task.Result["matches"].ToList<JToken>();                
            });
            return summonerMatchList;
        }

        public static string Key { get { return RIOT_API_KEY; } }
        public static string Server { get { return RIOT_API_SERVER; } }
    }
}
