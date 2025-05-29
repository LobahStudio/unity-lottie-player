using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public interface IPoolObject
{
    public bool IsActivated { get; set; }
    public IPoolObject Create(Transform parent);
    public void Activate();
    public void Release();
}
public abstract class BasePoolManager<T> : MonoBehaviour where T : IPoolObject
{
    public Transform objectParent;
    public GameObject baseObject;
    public List<T> currentPoolObjects;
    internal void Awake()
    {
        currentPoolObjects = new List<T>();
    }
    public T GetObject(Func<T,bool> isTrue = null,GameObject baseObjectPrefab = null) 
    {
        if (isTrue == null)
        {
            isTrue = (x) => true;
        }
        var availablePoolObject = currentPoolObjects.FirstOrDefault(x => !x.IsActivated && isTrue.Invoke(x));
        if (availablePoolObject == null)
        {
            availablePoolObject = CreateNewPool(baseObjectPrefab != null ? baseObjectPrefab : baseObject);
        }
        availablePoolObject.Activate();
        return availablePoolObject;
    }
    public virtual void Release(T videoPlayerToRelease) 
    {
        var foundedPoolObject = currentPoolObjects.FirstOrDefault(x => x.Equals(videoPlayerToRelease));
        if (foundedPoolObject == null)
        {
            return;
        }
        foundedPoolObject.Release();
    }
    public T CreateNewPool(GameObject baseObjectPrefab) 
    {
        var newObject = baseObjectPrefab.GetComponent<T>().Create(objectParent);
        currentPoolObjects.Add((T)newObject);
        return (T)newObject;
    }
}
