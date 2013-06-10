using System;

    public class ApiResultMessage : Message
    {
        public DataFieldDeserializer data;
        public new String msg;
        public int time;
    }


    public class DataFieldDeserializer
    {
        public String name;
        public int rating;
        public int rank;
        public int played;
        public int won;
        public int surrendered;
        public int gold;
        public int scrolls;
        public int lastgame;
        public int lastupdate;
    }