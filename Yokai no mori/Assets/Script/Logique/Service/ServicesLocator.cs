using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServicesLocator
{
    private static Dictionary<Type, object> services = new();

    public static void Register<TService>(TService service)
    {
        try
        {
            if (services[typeof(TService)] == null)
            {
                Debug.LogWarning("Le service")
            }

            services[typeof(TService)] = service;
            Debug.Log($"{service} a etais enregistrer");
        }
        catch
        {
            Debug.LogError($"{service} n'a pas etais enregistrer");
            services[typeof(TService)] = null;
        }
    }

    public static void Unregister<TService>(TService service)
    {
        try
        {
            services[typeof(TService)] = null;
            Debug.Log($"{service} a etais supprimer");
        }
        catch
        {
            Debug.LogWarning($"{service} n'a pas pu être supprimer");
        }
    }

    public static TService Get<TService>()
    {
        try
        {
            return (TService)services[typeof(TService)];
        }
        catch
        {
            Debug.LogError($"{services[typeof(TService)]} n'est pas dans la liste des service");
            return  default;
        }
    }
}
