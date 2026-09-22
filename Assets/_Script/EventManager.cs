using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Script.Events
{
    public static class EventManager
    {
        // Lưu trữ danh sách delegate lắng nghe theo từng kiểu Struct Event
        private static readonly Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Đăng ký lắng nghe sự kiện kiểu T
        /// </summary>
        public static void Subscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
            {
                eventTable[eventType] = Delegate.Combine(existingDelegate, listener);
            }
            else
            {
                eventTable[eventType] = listener;
            }
        }

        /// <summary>
        /// Hủy đăng ký lắng nghe sự kiện kiểu T
        /// </summary>
        public static void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
            {
                Delegate currentDel = Delegate.Remove(existingDelegate, listener);

                if (currentDel == null)
                {
                    eventTable.Remove(eventType);
                }
                else
                {
                    eventTable[eventType] = currentDel;
                }
            }
        }

        /// <summary>
        /// Bắn một sự kiện đi kèm dữ liệu
        /// </summary>
        public static void Raise<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
            {
                if (existingDelegate is Action<T> callback)
                {
                    callback.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// Dọn dẹp toàn bộ event (gọi khi đổi Scene hoặc kết thúc game)
        /// </summary>
        public static void ClearAll()
        {
            eventTable.Clear();
        }
    }
}