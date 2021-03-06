﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LeaderboardWebApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace LeaderboardWebApi.Controllers
{
    public class HighScore
    {
        public string Game { get; set; }
        public string Nickname { get; set; }
        public int Points { get; set; }
    }

    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/xml", "application/json")]
    [OpenApiTag("Leaderboard", Description = "New operations that should be only visible for version 3")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(LeaderboardController))]
    public class LeaderboardController : Controller
    {
        private readonly LeaderboardContext context;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<LeaderboardController> logger;

        public LeaderboardController(LeaderboardContext context, IFeatureManager featureManager,
            ILoggerFactory loggerFactory = null)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.logger = loggerFactory?.CreateLogger<LeaderboardController>();
        }

        // GET api/leaderboard
        /// <summary>
        /// Retrieve a list of leaderboard scores.
        /// </summary>
        /// <returns>List of high scores per game.</returns>
        /// <response code="200">The list was successfully retrieved.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HighScore>), 200)]
        public async Task<ActionResult<IEnumerable<HighScore>>> Get(int limit = 0)
        {
            logger?.LogInformation("Retrieving score list with a limit of {SearchLimit}.", limit);

            var scores = context.Scores
                .Select(score => new HighScore()
                {
                    Game = score.Game,
                    Points = score.Points,
                    Nickname = score.Gamer.Nickname
                });

            if (featureManager.IsEnabled(nameof(ApiFeatureFlags.LeaderboardListLimit)))
            {
                int searchLimit = limit;

                // This is a demo bug, supposedly "hard" to find
                do
                {
                    searchLimit--;
                }
                while (searchLimit != 0);
 
                scores = scores.Take(limit);
            }

            return Ok(await scores.ToListAsync().ConfigureAwait(false));
        }

        //[HttpGet("[action]")]
        //[FeatureGate("Beta")]
        //public IEnumerable<FeatureValue> Features()
        //{
        //    var section = Configuration.GetSection("FeatureManagement");
        //    return null;
        //}
    }

    public class FeatureValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
