using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServicesLocator
{
    private static Dictionary<Type, object> services = new();

    public static void Register<TService>(TService service)
    {
        if (service == null)
        {
            Debug.LogError("Impossible d'enregistrer un service null");
            return;
        }

        var type = typeof(TService);

        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"{type.Name} existe déja");
            return;
        }

        services[type] = service;
        Debug.Log($"{type.Name} a etais enregistrer");
    }

    public static void Unregister<TService>(TService service)
    {
        var type = typeof(TService);

        if(services.Remove(type))
        {
            Debug.Log($"{type.Name} a etais supprimer");
        }
        else
        {
            Debug.LogWarning($"{type.Name} n'a pas pu être supprimer");
        }
    }

    public static TService Get<TService>()
    {
        var type = typeof(TService);

        if (services.TryGetValue(type, out var service))
        {
            return (TService)service;
        }

        Debug.LogError($"{type.Name} n'est pas enregistré");
        return default;
    }
}
