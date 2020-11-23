using System.Collections.Generic;

namespace TwitchLeecher.Core.Models
{
    public class VideoComments
    {
        public string _next;
        public List<Comment> comments { get; set; }
    }

    public class Comment
    {
        public string _id { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string channel_id { get; set; }
        public string content_type { get; set; }
        public string content_id { get; set; }
        public float content_offset_seconds { get; set; }
        public Commenter commenter { get; set; }
        public string source { get; set; }
        public string state { get; set; }
        public Message message { get; set; }
        public bool more_replies { get; set; }
        public string _next { get; set; }
    }

    public class Commenter
    {
        public string display_name { get; set; }
        public string _id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string bio { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string logo { get; set; }
    }

    public class Emoticon
    {
        public string _id { get; set; }
        public int begin { get; set; }
        public int end { get; set; }
        public string emoticon_id { get; set; }
        public string emoticon_set_id { get; set; }
    }

    public class Fragment
    {
        public string text { get; set; }
        public Emoticon emoticon { get; set; }
    }

    public class UserBadge
    {
        public string _id { get; set; }
        public string version { get; set; }
    }

    public class Message
    {
        public Dictionary<string, object> data { get; set; }
        public string body { get; set; }
        public List<Emoticon> emoticons { get; set; }
        public List<Fragment> fragments { get; set; }
        public bool is_action { get; set; }
        public List<UserBadge> user_badges { get; set; }
        public string user_color { get; set; }
    }
}
