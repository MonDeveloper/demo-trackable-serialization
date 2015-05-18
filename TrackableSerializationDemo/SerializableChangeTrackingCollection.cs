using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using TrackableEntities;
using TrackableEntities.Client;

namespace TrackableSerializationDemo
{
    public class SerializableChangeTrackingCollection<TEntity> : ChangeTrackingCollection<TEntity>
        where TEntity : class, ITrackable, INotifyPropertyChanged
    {
        public SerializableChangeTrackingCollection() { }

        public SerializableChangeTrackingCollection(
            IEnumerable<TEntity> entities, bool disableTracking = false)
            : base(entities, disableTracking) { }

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            RestoreDeletes();
        }

        [OnSerialized]
        internal void OnSerialized(StreamingContext context)
        {
            RemoveDeletes(true);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            RemoveDeletes(false);
        }

        private void RestoreDeletes()
        {
            bool tracking = Tracking;
            Tracking = false;

            var deletedEntities = this.GetCachedDeletes();
            foreach (var entity in deletedEntities)
                Add(entity);

            Tracking = tracking;
        }

        private void RemoveDeletes(bool cached)
        {
            bool tracking = Tracking;
            Tracking = !cached;

            for (int i = Count - 1; i > -1; i--)
            {
                if (this[i].TrackingState == TrackingState.Deleted)
                    RemoveAt(i);
            }

            Tracking = tracking;
        }
    }
}
