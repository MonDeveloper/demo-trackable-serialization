using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using TrackableEntities;
using TrackableEntities.Client;

namespace TrackableSerializationDemo
{
    public static class ChangeTrackingExtensions
    {
        public static Collection<TEntity> GetCachedDeletes<TEntity>
            (this ChangeTrackingCollection<TEntity> changeTracker)
            where TEntity : class, ITrackable, INotifyPropertyChanged
        {
            var deletedField = typeof(ChangeTrackingCollection<TEntity>)
                .GetField("_deletedEntities", BindingFlags.Instance | BindingFlags.NonPublic);
            var deletedEntities = (Collection<TEntity>)deletedField.GetValue(changeTracker);
            return deletedEntities;
        }
    }
}
