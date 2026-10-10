using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace OsuReloadPoc
{
    // All access happens on osu!'s scheduler thread. Tokens belong to the guarded MVID.
    internal sealed class EditorCircle
    {
        private readonly Module module;
        private object sprite;
        private object host;
        private object editor;
        private Delegate updateCallback;
        private long updates;
        private float radius;
        private float x;
        private float y;
        private string updateError;

        internal EditorCircle(Module module)
        {
            this.module = module;
        }

        internal string Show(object activeEditor, float x, float y, float requestedRadius)
        {
            if (requestedRadius <= 0) throw new ArgumentException("Radius must be positive.");
            Clear();
            object newHost = Field(0x040022FA).GetValue(activeEditor); // Editor.uiManager, drawn last.
            if (newHost == null) throw new InvalidOperationException("The editor's UI sprite manager is unavailable.");

            var constructor = (ConstructorInfo)module.ResolveMethod(0x06003F09);
            ParameterInfo[] parameters = constructor.GetParameters();
            object newSprite = constructor.Invoke(new[]
            {
                (object)"approachcircle", Vector(x, y),
                Enum.ToObject(parameters[2].ParameterType, 1), // SkinSource.Osu: built-in texture.
                Enum.ToObject(parameters[3].ParameterType, 1), // Origins.Centre.
                Enum.ToObject(parameters[4].ParameterType, 5), // Fields.Native; positions converted by GameField.
            });
            try
            {
                if (Field(0x040028A5).GetValue(newSprite) == null)
                    throw new InvalidOperationException("osu!'s built-in approach-circle texture is unavailable.");
                int width = (int)Field(0x0400289D).GetValue(newSprite);
                if (width <= 0) throw new InvalidOperationException("The circle texture has no display width.");
                SetGeometry(newSprite, x, y, requestedRadius, width);
                Field(0x04000F3B).SetValue(newSprite, 1f); // Depth, before sorted insertion.
                Field(0x04000F43).SetValue(newSprite, false); // HandleInput: click-through.
                Field(0x04000F53).SetValue(newSprite, false); // Shared built-in texture is not ours to dispose.
                Type colourType = Field(0x04000F3F).FieldType;
                object colour = Activator.CreateInstance(colourType, new object[] { (byte)0, (byte)255, (byte)255, (byte)255 });
                Field(0x04000F3F).SetValue(newSprite, colour);
                Action onUpdate = delegate
                {
                    updates++;
                    // Avoid letting a diagnostic overlay failure escape into osu!'s draw loop.
                    try { SetGeometry(newSprite, this.x, this.y, requestedRadius, width); }
                    catch (Exception exception)
                    {
                        updateError = exception.GetBaseException().Message;
                        Field(0x04000F49).SetValue(newSprite, 0f);
                    }
                };
                var addUpdate = (MethodInfo)module.ResolveMethod(0x06003F13);
                updateCallback = Delegate.CreateDelegate(addUpdate.GetParameters()[0].ParameterType, onUpdate.Target, onUpdate.Method);
                addUpdate.Invoke(newSprite, new object[] { updateCallback });
                module.ResolveMethod(0x06000A78).Invoke(newHost, new[] { newSprite });
            }
            catch
            {
                if (updateCallback != null)
                    module.ResolveMethod(0x06003F14).Invoke(newSprite, new object[] { updateCallback });
                updateCallback = null;
                throw;
            }
            sprite = newSprite;
            host = newHost;
            editor = activeEditor;
            radius = requestedRadius;
            this.x = x;
            this.y = y;
            updateError = null;
            updates = 0;
            return "OK circle added " + StatusDetails();
        }

        internal string Move(float x, float y)
        {
            if (sprite == null) throw new InvalidOperationException("No circle is active.");
            SetGeometry(sprite, x, y, radius, (int)Field(0x0400289D).GetValue(sprite));
            this.x = x;
            this.y = y;
            return "OK circle moved " + StatusDetails();
        }

        internal string Status()
        {
            return sprite == null ? "OK overlay=none" : "OK " + StatusDetails();
        }

        internal string Clear()
        {
            if (sprite == null) return "OK overlay=none";
            module.ResolveMethod(0x06003F14).Invoke(sprite, new object[] { updateCallback });
            module.ResolveMethod(0x06000A7F).Invoke(host, new[] { sprite });
            bool removed = !((IList)Field(0x04000754).GetValue(host)).Contains(sprite);
            sprite = null;
            host = null;
            editor = null;
            updateCallback = null;
            return "OK circle removed removed=" + removed + " updates=" + updates;
        }

        internal void RemoveIfEditorChanged()
        {
            if (sprite != null && (!ReferenceEquals(editor, Field(0x04001396).GetValue(null))
                || !ReferenceEquals(host, Field(0x040022FA).GetValue(editor))
                || !((IList)Field(0x04000754).GetValue(host)).Contains(sprite)))
                Clear();
        }

        private void SetGeometry(object targetSprite, float targetX, float targetY, float requestedRadius, int width)
        {
            object gamefield = Field(0x0400138C).GetValue(null);
            float fieldRatio = (float)module.ResolveMethod(0x0600381E).Invoke(gamefield, null);
            object[] position = { Vector(targetX, targetY) };
            module.ResolveMethod(0x06003829).Invoke(gamefield, position); // GameField.FieldToDisplay(ref Vector2).
            Field(0x04000F4A).SetValue(targetSprite, position[0]);
            Field(0x04000F40).SetValue(targetSprite, position[0]);
            Field(0x04000F48).SetValue(targetSprite, requestedRadius * 2 / width * fieldRatio);
        }

        private string StatusDetails()
        {
            bool present = ((IList)Field(0x04000754).GetValue(host)).Contains(sprite);
            return string.Format(CultureInfo.InvariantCulture,
                "overlay=circle x={0} y={1} radius={2} present={3} visible={4} updates={5} managerFrame={6} updateError={7} " +
                "nativePosition={8} scale={9} drawScale={10} colour={11} rectangle={12} textureDisposed={13} textureUploaded={14}",
                x, y, radius, present, Field(0x04000F4B).GetValue(sprite), updates, Field(0x04000746).GetValue(host), updateError ?? "none",
                Field(0x04000F4A).GetValue(sprite), Field(0x04000F48).GetValue(sprite), Field(0x040028B4).GetValue(sprite),
                Field(0x04002894).GetValue(sprite), Field(0x040028A8).GetValue(sprite),
                module.ResolveMethod(0x06002F31).Invoke(Field(0x040028A5).GetValue(sprite), null),
                Field(0x04001ECF).GetValue(Field(0x040028A5).GetValue(sprite)) != null);
        }

        private object Vector(float x, float y)
        {
            return Activator.CreateInstance(Field(0x04000F4A).FieldType, new object[] { x, y });
        }

        private FieldInfo Field(int token)
        {
            return module.ResolveField(token);
        }
    }
}
