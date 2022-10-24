using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System;

namespace rg_gui
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool m_suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!m_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }

        public void Reset(IEnumerable<T> contents)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            m_suppressNotification = true;

            Clear();
            foreach (T item in contents)
            {
                Add(item);
            }

            m_suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}