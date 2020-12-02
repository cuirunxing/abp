﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Components.WebAssembly
{
    public class AbpBlazorClientHttpMessageHandler : DelegatingHandler, ITransientDependency
    {
        private readonly IJSRuntime _jsRuntime;

        private readonly ICookieService _cookieService;

        private readonly NavigationManager _navigationManager;

        private const string AntiForgeryCookieName = "XSRF-TOKEN";

        private const string AntiForgeryHeaderName = "RequestVerificationToken";

        public AbpBlazorClientHttpMessageHandler(
            IJSRuntime jsRuntime,
            ICookieService cookieService,
            NavigationManager navigationManager)
        {
            _jsRuntime = jsRuntime;
            _cookieService = cookieService;
            _navigationManager = navigationManager;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SetLanguageAsync(request, cancellationToken);
            await SetAntiForgeryTokenAsync(request);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task SetLanguageAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var selectedLanguage = await _jsRuntime.InvokeAsync<string>(
                "localStorage.getItem",
                cancellationToken,
                "Abp.SelectedLanguage"
            );

            if (!selectedLanguage.IsNullOrWhiteSpace())
            {
                request.Headers.AcceptLanguage.Clear();
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(selectedLanguage));
            }
        }

        private async Task SetAntiForgeryTokenAsync(HttpRequestMessage request)
        {
            var selfUri = new Uri(_navigationManager.Uri);

            Console.WriteLine("----------"+selfUri);
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.RequestUri.Host != selfUri.Host || request.RequestUri.Port != selfUri.Port)
            {
                return;
            }

            var token = await _cookieService.GetAsync(AntiForgeryCookieName);
            if (!token.IsNullOrWhiteSpace())
            {
                request.Headers.Add(AntiForgeryHeaderName, token);
            }
        }
    }
}
