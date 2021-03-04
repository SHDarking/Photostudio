using System;
using Microsoft.Extensions.Configuration;

namespace Photostudio.DAO
{
    public abstract class DAOFactory
    {
        
        public static IMyDAO GetDAO(TypeDatabases type)
        {
            switch (type)
            {
                case TypeDatabases.MongoDB : return new MongoDAO(Startup.Configuration);
                default: throw  new Exception("Another realizations of databases not found.");
            }
        }
    }
}