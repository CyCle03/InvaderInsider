using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀링 시스템
    /// 투사체, 이펙트 등의 빈번한 생성/제거로 인한 GC 압박을 줄입니다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly HashSet<T> activeObjects = new HashSet<T>();
        private readonly int maxPoolSize;
        private readonly bool expandPool;

        public int AvailableCount => availableObjects.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;

        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, bool expandPool = true, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxPoolSize = maxSize;
            this.expandPool = expandPool;

            // 초기 풀 생성
            PrewarmPool(initialSize);
        }

        /// <summary>
        /// 풀을 초기 크기로 미리 생성
        /// </summary>
        private void PrewarmPool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 새로운 오브젝트 생성 및 풀에 추가
        /// </summary>
        private T CreateNewObject()
        {
            T newObj = Object.Instantiate(prefab, parent);
            newObj.gameObject.SetActive(false);
            availableObjects.Enqueue(newObj);
            return newObj;
        }

        /// <summary>
        /// 풀에서 오브젝트 가져오기
        /// </summary>
        public T GetObject()
        {
            T obj = null;

            // 사용 가능한 오브젝트가 있는 경우
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            // 풀이 비어있고 확장 가능한 경우
            else if (expandPool && TotalCount < maxPoolSize)
            {
                obj = CreateNewObject();
            }
            // 풀이 가득 찬 경우 null 반환
            else
            {
                return null;
            }

            // 가져온 오브젝트가 유효한지 확인
            if (obj == null || obj.gameObject == null)
            {
                return GetObject();
            }

            activeObjects.Add(obj);
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnObject(T obj)
        {
            if (obj == null) return;

            if (activeObjects.Remove(obj))
            {
                obj.gameObject.SetActive(false);
                
                if (parent != null)
                {
                    obj.transform.SetParent(parent);
                }
                
                availableObjects.Enqueue(obj);
            }
            else
            {
                // This can happen if an object is returned to the pool twice.
            }
        }

        /// <summary>
        /// 모든 활성 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnAllActiveObjects()
        {
            var activeList = new List<T>(activeObjects);
            foreach (var obj in activeList)
            {
                ReturnObject(obj);
            }
        }

        /// <summary>
        /// 풀 정리 - 사용하지 않는 오브젝트 제거
        /// </summary>
        public void Clear()
        {
            // 활성 오브젝트들 먼저 반환
            ReturnAllActiveObjects();

            // 사용 가능한 오브젝트들 제거
            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Object.DestroyImmediate(obj.gameObject);
                }
            }

            activeObjects.Clear();
        }

        /// <summary>
        /// 풀 상태 정보 출력 (디버깅용)
        /// </summary>
        public void LogPoolStatus()
        {
            // Debug.LogFormat(...)
        }
    }
}