﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace DiscordBot
{
    public class BotData
    {
        public DateTime LastPublisherCheck { get; set; }
        public List<ulong> LastPublisherId { get; set; }
    }

    public class UserData
    {
        public Dictionary<ulong, DateTime> MutedUsers { get; set; }
        public Dictionary<ulong, DateTime> ThanksReminderCooldown { get; set; }
        public Dictionary<ulong, DateTime> CodeReminderCooldown { get; set; }

        public UserData()
        {
            MutedUsers = new Dictionary<ulong, DateTime>();
            ThanksReminderCooldown = new Dictionary<ulong, DateTime>();
            CodeReminderCooldown = new Dictionary<ulong, DateTime>();
        }
    }

    //TODO: Download all avatars to cache them

    public class UpdateService
    {
        private readonly LoggingService _loggingService;
        private readonly PublisherService _publisherService;
        private readonly DatabaseService _databaseService;
        public readonly UserService _userService;
        private readonly AnimeService _animeService;
        private readonly CancellationToken _token;
        private BotData _botData;
        private Random _random;
        private AnimeData _animeData;
        private UserData _userData;

        public UpdateService(LoggingService loggingService, PublisherService publisherService, DatabaseService databaseService, UserService userService, AnimeService animeService)
        {
            _loggingService = loggingService;
            _publisherService = publisherService;
            _databaseService = databaseService;
            _userService = userService;
            _animeService = animeService;
            _token = new CancellationToken();
            _random = new Random();

            UpdateLoop();
        }

        private void UpdateLoop()
        {
            ReadDataFromFile();
            SaveDataToFile();
            //CheckDailyPublisher();
            UpdateUserRanks();
            UpdateAnime();
        }

        private void ReadDataFromFile()
        {
            if (File.Exists($"{Settings.GetServerRootPath()}/botdata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/botdata.json");
                _botData = JsonConvert.DeserializeObject<BotData>(json);
            }
            else
                _botData = new BotData();

            if (File.Exists($"{Settings.GetServerRootPath()}/animedata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/animedata.json");
                _animeData = JsonConvert.DeserializeObject<AnimeData>(json);
            }
            else
                _animeData = new AnimeData();

            if (File.Exists($"{Settings.GetServerRootPath()}/userdata.json"))
            {
                string json = File.ReadAllText($"{Settings.GetServerRootPath()}/userdata.json");
                _userData = JsonConvert.DeserializeObject<UserData>(json);
            }
            else
                _userData = new UserData();
        }

        /*
        ** Save data to file every 20s
        */

        private async Task SaveDataToFile()
        {
            while (true)
            {
                var json = JsonConvert.SerializeObject(_botData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/botdata.json", json);

                json = JsonConvert.SerializeObject(_animeData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/animedata.json", json);

                json = JsonConvert.SerializeObject(_userData);
                File.WriteAllText($"{Settings.GetServerRootPath()}/userdata.json", json);
                //await _logging.LogAction("Data successfully saved to file", true, false);
                await Task.Delay(TimeSpan.FromSeconds(20d), _token);
            }
        }

        public async Task CheckDailyPublisher(bool force = false)
        {
            await Task.Delay(TimeSpan.FromSeconds(10d), _token);
            while (true)
            {
                if (_botData.LastPublisherCheck < DateTime.Now - TimeSpan.FromDays(1d) || force)
                {
                    uint count = _databaseService.GetPublisherAdCount();
                    ulong id;
                    uint rand;
                    do
                    {
                        rand = (uint) _random.Next((int) count);
                        id = _databaseService.GetPublisherAd(rand).userId;
                    } while (_botData.LastPublisherId.Contains(id));

                    await _publisherService.PostAd(rand);
                    await _loggingService.LogAction("Posted new daily publisher ad.", true, false);
                    _botData.LastPublisherCheck = DateTime.Now;
                    _botData.LastPublisherId.Add(id);
                }

                if (_botData.LastPublisherId.Count > 10)
                    _botData.LastPublisherId.RemoveAt(0);

                if (force)
                    return;
                await Task.Delay(TimeSpan.FromMinutes(5d), _token);
            }
        }

        private async Task UpdateUserRanks()
        {
            await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            while (true)
            {
                _databaseService.UpdateUserRanks();
                await Task.Delay(TimeSpan.FromMinutes(1d), _token);
            }
        }

        private async Task UpdateAnime()
        {
            await Task.Delay(TimeSpan.FromSeconds(30d), _token);
            while (true)
            {
                if (_animeData.LastDailyAnimeAiringList < DateTime.Now - TimeSpan.FromDays(1d))
                {
                    _animeService.PublishDailyAnime();
                    _animeData.LastDailyAnimeAiringList = DateTime.Now;
                }

                if (_animeData.LastWeeklyAnimeAiringList < DateTime.Now - TimeSpan.FromDays(7d))
                {
                    _animeService.PublishWeeklyAnime();
                    _animeData.LastWeeklyAnimeAiringList = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromMinutes(1d), _token);
            }
        }

        public UserData GetUserData()
        {

            return _userData;
        }

        public void SetUserData(UserData data)
        {
            _userData = data;
        }
    }
}