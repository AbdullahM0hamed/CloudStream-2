using CloudStreamForms.Core.BaseProviders;
using System;
using System.Collections.Generic;
using System.Net;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class FastAniBFProvider : BloatFreeBaseAnimeProvider
	{
		public override string Name => "Fastani";

		[System.Serializable]
		public struct FastAniData
		{
			public string href;
			public string title;
		}

		public override object StoreData(string year, TempThread tempThread, MALData malData)
		{
			Search(malData.engName);
		}

		private struct TokenData
		{
			private string key;
			private string val;
			private string cookies;
		}

		private TokenData GetTokenData()
		{
			string response = DownloadString(
			return new TokenData() { key = key, val = val, cookies = cookies };
		}


		private List<FastAniData> Search(string query)
		{
			TokenData tokenData = GetTokenData();
