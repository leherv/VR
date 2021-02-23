using System.Collections.Generic;

namespace BusinessEntities
{
    public class NotificationInfo
    {
        public string Message { get; set; }
        public string MediaName { get; set; }
        public List<string> NotificationEndpointIdentifiers { get; set; }

        public NotificationInfo(string message, string mediaName, List<string> notificationEndpointIdentifiers)
        {
            Message = message;
            MediaName = mediaName;
            NotificationEndpointIdentifiers = notificationEndpointIdentifiers;
        }
        
        public NotificationInfo(int chapterNumber, int subChapterNumber, string url, List<string> notificationEndpointIdentifiers, string mediaName)
        {
            Message = BuildMessage(chapterNumber, subChapterNumber, url);
            MediaName = mediaName;
            NotificationEndpointIdentifiers = notificationEndpointIdentifiers;
        }

        private static string BuildMessage(int chapterNumber, int subChapterNumber, string url)
        {
            var message = $"Chapter {chapterNumber.ToString()}";
            message += subChapterNumber > 0
                ? $".{subChapterNumber.ToString()} "
                : " ";
            message += $"is here! Check it out at: {url}";
            return message;
        }
    }
}