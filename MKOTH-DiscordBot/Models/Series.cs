﻿using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace MKOTHDiscordBot.Models
{
    public class Series
    {
        [BsonId]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string WinnerId { get; set; }
        public string LoserId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public string ReplayId { get; set; }
    }
}
