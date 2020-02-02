﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Models;
using MKOTHDiscordBot.Properties;

using RestSharp;

namespace MKOTHDiscordBot.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly string endPoint;
        private readonly string adminKey;
        //private readonly RankingService rankingService;

        private readonly RestClient restClient;

        private List<Series> seriesList;
        private List<Series> pendingList;

        private int? nextId = null;

        public int NextId => nextId.HasValue ? (int)(nextId = nextId.Value + 1) : (seriesList.Count > 0 ? (int)(nextId = seriesList.Max(x => x.Id) + 1) : 0);
        public IEnumerable<ulong> AllPlayers => seriesList.Select(x => ulong.Parse(x.WinnerId)).Concat(seriesList.Select(x => ulong.Parse(x.LoserId))).Distinct();
        public IEnumerable<Series> SeriesHistory => seriesList;

        private const string collectionName = "_series";

        public event Func<Task> Updated;

        public bool Ready { get; private set; } = false;

        public SeriesService(IServiceProvider services, IOptions<AppSettings> appSettings, IOptions<Credentials> credentials)
        {
            //rankingService = services.GetService<RankingService>();
            endPoint = appSettings.Value.ConnectionStrings.AppsScript;
            adminKey = credentials.Value.AppsScriptAdminKey;

            restClient = new RestClient(endPoint);

            pendingList = new List<Series>();

            _ = RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            var request = new RestRequest()
                //.AddQueryParameter("admin", adminKey)
                .AddQueryParameter("spreadSheet", collectionName)
                .AddQueryParameter("operation", "all");
            var response = await restClient.GetAsync<List<Series>>(request);

            seriesList = response;

            Ready = true;
            _ = Updated.Invoke();

            Logger.Debug(response.Count, "Series Service Refresh Data Size");
        }

        public async Task PostAsync()
        {
            var request = new RestRequest()
                   .AddQueryParameter("admin", adminKey)
                   .AddQueryParameter("spreadSheet", collectionName)
                   .AddQueryParameter("operation", "all")
                   .AddJsonBody(seriesList);
            var response = await restClient.PostAsync<dynamic>(request);

            _ = Updated.Invoke();

            Logger.Debug(response, "Series Service Update");
        }

        public Series MakeSeries(ulong winner, ulong loser, int wins, int losses, int draws, string replay)
        {
            return new Series
            {
                Id = NextId,
                Date = DateTime.Now,
                WinnerId = winner.ToString(),
                LoserId = loser.ToString(),
                Wins = wins,
                Losses = losses,
                Draws = draws,
                ReplayId = replay
            };
        }

        public void AddPending(Series series)
        {
            pendingList.Add(series);
        }

        public async Task ApproveAsync(int id)
        {
            var series = pendingList.Find(x => x.Id == id);

            if (series == default)
            {
                return;
            }

            pendingList.Remove(series);
            seriesList.Add(series);
            await PostAsync();
        }

        public async Task AdminCreateAsync(Series series)
        {
            seriesList.Add(series);
            await PostAsync();
        }

        public async Task RemoveAsync(int id)
        {
            var series = seriesList.Find(x => x.Id == id);
            seriesList.Remove(series);
            await PostAsync();
            await RefreshAsync();
        }

        public bool HasNewPlayer(Series series)
        {
            if (seriesList.Count(x => series.WinnerId == x.WinnerId || series.WinnerId == x.LoserId) == 0)
            {
                return true;
            }
            if (seriesList.Count(x => series.LoserId == x.LoserId || series.LoserId == x.WinnerId) == 0)
            {
                return true;
            }
            return false;
        }

        public bool CanApprove(Series series, IGuildUser user)
        {
            if (HasNewPlayer(series))
            {
                return user.GuildPermissions.Administrator;
            }
            else
            {
                return series.LoserId == user.Id.ToString()|| user.GuildPermissions.Administrator;
            }
        }

        public IEnumerable<string> LastSeriesHistoryLines(int count)
        {
            var currentDate = DateTime.Now;
            foreach (var series in seriesList.AsEnumerable().Reverse().Take(count).Reverse())
            {
                yield return $"`#{series.Id.ToString("D4")}` {(currentDate - series.Date).AsRoundedDuration()} ago <@!{series.WinnerId}> <@!{series.LoserId}> {series.Wins} {series.Losses}";
            }
        }
    }

    public interface ISeriesService
    {
        int NextId { get; }

        void AddPending(Series series);
        Task AdminCreateAsync(Series series);
        Task ApproveAsync(int id);
        bool CanApprove(Series series, IGuildUser user);
        bool HasNewPlayer(Series series);
        Series MakeSeries(ulong winner, ulong loser, int wins, int losses, int draws, string replay);
        Task PostAsync();
        Task RefreshAsync();
        Task RemoveAsync(int id);
    }
}
