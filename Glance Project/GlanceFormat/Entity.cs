using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlanceFormat
{
    public class Entity
    {
        public string name;
        public Texture2D texture;

        public Entity(string name, Texture2D texture)
        {
            this.name = name;
            this.texture = texture;
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            
        }
        public virtual void Update(GameTime gameTime)
        {

        }
    }
}
