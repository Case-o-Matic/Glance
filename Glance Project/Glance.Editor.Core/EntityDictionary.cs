using GlanceFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glance.Editor.Core
{
    public class EntityDictionary
    {
        private Dictionary<string, Entity> entities;
        private string loadFile;

        private Entity cachedEntity;
        public Entity this[string key]
        {
            get
            {
                if (cachedEntity.name != key)
                    entities.TryGetValue(key, out cachedEntity);

                return CloneEntity(cachedEntity);
            }
        }

        public EntityDictionary(string loadFile)
        {
            entities = new Dictionary<string, Entity>();
            this.loadFile = loadFile;

            LoadFromFile();
        }

        public void AddEntity(Entity entity)
        {
            if (!entities.ContainsValue(entity))
            {
                entities.Add(entity.name, entity);
            }
        }
        public void RemoveEntity(Entity entity)
        {
            entities.Remove(entity.name);
        }

        public void SaveToFile()
        {

        }
        public void LoadFromFile()
        {
            
        }

        private Entity CloneEntity(Entity entity)
        {
            return new Entity(entity.name, entity.texture);
        }
    }
}
