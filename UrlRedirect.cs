using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cloud5mins.domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Cloud5mins.Function
{
    public class UrlRedirect
    {
        private NLogWrapper logger;
        private readonly ShortenerSettings _shortenerSettings;

        public UrlRedirect(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            logger = new NLogWrapper(LoggerType.UrlRedirect, settings);
            _shortenerSettings = settings;
        }

        [Function("UrlRedirect")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "urlredirect/{shortUrl}")]
            HttpRequestData req,
            string shortUrl,
            ExecutionContext context)
        {
            string redirectUrl = "https://azure.com";


            if (!String.IsNullOrWhiteSpace(shortUrl))
            {
                redirectUrl = _shortenerSettings.defaultRedirectUrl;

                StorageTableHelper stgHelper = new StorageTableHelper(_shortenerSettings.UlsDataStorage);

                var tempUrl = new ShortUrlEntity(string.Empty, shortUrl);
                var newUrl = await stgHelper.GetShortUrlEntity(tempUrl);

                if (newUrl != null)
                {
                    logger.Log(NLog.LogLevel.Info, "Redirect url found for short url {url.shortUrl} : {url.redirectUrl}", shortUrl, newUrl.Url);
                    newUrl.Clicks++;
                    await stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey));
                    await stgHelper.SaveShortUrlEntity(newUrl);
                    redirectUrl = WebUtility.UrlDecode(newUrl.ActiveUrl);
                }
                else
                {
                    logger.Log(NLog.LogLevel.Warn, "Redirect url not found for short url : {url.shortUrl}", shortUrl);
                }
            }
            else
            {
                logger.Log(NLog.LogLevel.Error, "Bad Link, resorting to fallback.");
            }

            var res = req.CreateResponse(HttpStatusCode.Redirect);
            res.Headers.Add("Location", redirectUrl);
            return res;

        }
    }
}
