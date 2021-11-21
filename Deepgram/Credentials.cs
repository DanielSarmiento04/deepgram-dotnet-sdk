﻿using System;

namespace Deepgram
{
    public class Credentials
    {
        public Credentials()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="apiKey">Deepgram API Key</param>
        /// <param name="apiUrl">Uri of Deepgram API</param>
        public Credentials(string? apiKey = null, string? apiUrl = null)
        {
            ApiKey = apiKey;
            ApiUrl = apiUrl;
        }

        /// <summary>
        /// Deepgram API Key
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// On-premise Url of the Deepgram API
        /// </summary>
        public string? ApiUrl { get; set; }

    }
}