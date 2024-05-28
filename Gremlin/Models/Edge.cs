﻿using n_ate.Essentials.Models;
using n_ate.Gremlin.Contracts;

namespace n_ate.Gremlin.Models
{
    public class Edge : TrackingBase, IDatabaseModel
    {
        private string _createdBy = string.Empty;
        private long _creationDate = LongDateTime.UtcNow.Value;
        private long _effectiveEndDate = LongDateTime.UtcMax.Value;
        private long _effectiveStartDate = LongDateTime.UtcNow.Value;
        private string _id = string.Empty;
        private string _label = string.Empty;
        private long _lastUpdateDate = LongDateTime.UtcNow.Value;
        private string _lastUpdatedBy = string.Empty;

        public string CreatedBy
        {
            get { return _createdBy; }
            set { PropertyChanging(value); _createdBy = value; }
        }

        public long creationDate
        {
            get { return _creationDate; }
            set { PropertyChanging(value); _creationDate = value; }
        }

        public long effectiveEndDate
        {
            get { return _effectiveEndDate; }
            set { PropertyChanging(value); _effectiveEndDate = value; }
        }

        public long effectiveStartDate
        {
            get { return _effectiveStartDate; }
            set { PropertyChanging(value); _effectiveStartDate = value; }
        }

        public string Id
        {
            get { return _id; }
            set { PropertyChanging(value); _id = value; }
        }

        public string Label
        {
            get { return _label; }
            set { PropertyChanging(value); _label = value; }
        }

        public long lastUpdateDate
        {
            get { return _lastUpdateDate; }
            set { PropertyChanging(value); _lastUpdateDate = value; }
        }

        public string LastUpdatedBy
        {
            get { return _lastUpdatedBy; }
            set { PropertyChanging(value); _lastUpdatedBy = value; }
        }

        internal static Edge? NONE { get; private set; }

        internal class NoEdge : Edge
        {
            private NoEdge()
            { }

            ~NoEdge()
            {
                NONE = new NoEdge();
            }
        }
    }
}