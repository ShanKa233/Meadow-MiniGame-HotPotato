using RWCustom;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato.Effect
{
    public class PieceOfSlug: UpdatableAndDeletable, IDrawable
    {
        public float Rad
        {
            get
            {
                return 4f * this.massLeft;
            }
        }
        public int JaggedSprite
        {
            get
            {
                return 0;
            }
        }
        public int SlimeSprite(int s)
        {
            return 1 + s;
        }
        public int DotSprite
        {
            get
            {
                return 1 + this.slime.GetLength(0);
            }
        }
        public int TotalSprites
        {
            get
            {
                return this.slime.GetLength(0) + 2;
            }
        }

        public PieceOfSlug(Vector2 pos, Vector2 vel, Player player)
        {
            this.lastPos = pos;
            this.vel = vel;
            this.pos = pos + vel;
            this.player = player;

            

            this.fallOff = Random.Range(2, 100);
            this.massLeft = 1f;
            this.dissapearSpeed = Random.value/2f;

            this.slime = new Vector2[(int)Mathf.Lerp(8f, 15f, Random.value), 4];

            for (int i = 0; i < this.slime.GetLength(0); i++)
            {
                this.slime[i, 0] = pos + Custom.RNV() * 4f * Random.value;
                this.slime[i, 1] = this.slime[i, 0];
                this.slime[i, 2] = vel + Custom.RNV() * 4f * Random.value;
                int num;
                if (i == 0 || Random.value < 0.3f)
                {
                    num = -1;
                }
                else if (Random.value < 0.7f)
                {
                    num = i - 1;
                }
                else
                {
                    num = Random.Range(0, this.slime.GetLength(0));
                }
                this.slime[i, 3] = new Vector2((float)num, Mathf.Lerp(3f, 8f, Random.value));
            }
        }

        public override void Update(bool eu)
        {
            this.lastPos = this.pos;
            this.pos += this.vel;
            this.vel.y -= room.gravity*0.6f;

            for (int i = 0; i < this.slime.GetLength(0); i++)
            {
                this.slime[i, 1] = this.slime[i, 0];
                this.slime[i, 0] += this.slime[i, 2];
                this.slime[i, 2] *= 0.99f;
                this.slime[i, 2].y -= 0.9f * (ModManager.MMF ? this.room.gravity : 1f);

                SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(this.slime[i, 0], this.slime[i, 1], this.slime[i, 2], 1, default(IntVector2), true);
                terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                this.slime[i, 0] = terrainCollisionData.pos;
                this.slime[i, 2] = terrainCollisionData.vel;    

                if ((int)this.slime[i, 3].x < 0 || (int)this.slime[i, 3].x >= this.slime.GetLength(0))
                {
                    Vector2 vector = this.pos;
                    Vector2 a = Custom.DirVec(this.slime[i, 0], vector);
                    float num = Vector2.Distance(this.slime[i, 0], vector);
                    this.slime[i, 0] -= a * (this.slime[i, 3].y * this.massLeft - num) * 0.9f;
                    this.slime[i, 2] -= a * (this.slime[i, 3].y * this.massLeft - num) * 0.9f;
                }
                else
                {
                    Vector2 a2 = Custom.DirVec(this.slime[i, 0], this.slime[(int)this.slime[i, 3].x, 0]);
                    float num2 = Vector2.Distance(this.slime[i, 0], this.slime[(int)this.slime[i, 3].x, 0]);
                    this.slime[i, 0] -= a2 * (this.slime[i, 3].y * this.massLeft - num2) * 0.5f;
                    this.slime[i, 2] -= a2 * (this.slime[i, 3].y * this.massLeft - num2) * 0.5f;
                    this.slime[(int)this.slime[i, 3].x, 0] += a2 * (this.slime[i, 3].y * this.massLeft - num2) * 0.5f;
                    this.slime[(int)this.slime[i, 3].x, 2] += a2 * (this.slime[i, 3].y * this.massLeft - num2) * 0.5f;
                }
            }
            if (this.stickChunk != null && this.stickChunk.owner is Creature && (this.stickChunk.owner as Creature).inShortcut)
            {
                this.stickChunk = null;
            }
            if (this.stickChunk != null && this.stickChunk.owner.room == this.room && Custom.DistLess(this.stickChunk.pos, this.pos, this.stickChunk.rad + 40f) && this.fallOff > 0)
            {
                float num3 = this.Rad + this.stickChunk.rad;
                Vector2 a3 = Custom.DirVec(this.pos, this.stickChunk.pos);
                float num4 = Vector2.Distance(this.pos, this.stickChunk.pos);
                float num5 = this.stickChunk.mass / (0.1f * this.massLeft + this.stickChunk.mass);
                this.pos += a3 * (num4 - num3) * num5;
                this.vel += a3 * (num4 - num3) * num5;
                this.stickChunk.pos -= a3 * (num4 - num3) * (1f - num5);
                this.stickChunk.vel -= a3 * (num4 - num3) * (1f - num5);
                this.fallOff--;
            }
            else
            {
                this.stickChunk = null;
                bool flag = false;
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(this.room, this.room.GetTilePosition(this.lastPos), this.room.GetTilePosition(this.pos));
                if (intVector != null)
                {
                    FloatRect floatRect = Custom.RectCollision(this.pos, this.lastPos, this.room.TileRect(intVector.Value).Grow(this.Rad));
                    this.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                    {
                        this.vel.x = Mathf.Abs(this.vel.x) * 0.2f;
                        this.vel.y = this.vel.y * 0.8f;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                    {
                        this.vel.x = -Mathf.Abs(this.vel.x) * 0.2f;
                        this.vel.y = this.vel.y * 0.8f;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                    {
                        this.vel.y = Mathf.Abs(this.vel.y) * 0.2f;
                        this.vel.x = this.vel.x * 0.8f;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                    {
                        this.vel.y = -Mathf.Abs(this.vel.y) * 0.2f;
                        this.vel.x = this.vel.x * 0.8f;
                        flag = true;
                    }
                }
                if (!flag)
                {
                    Vector2 vector2 = this.vel;
                    SharedPhysics.TerrainCollisionData terrainCollisionData = this.scratchTerrainCollisionData.Set(this.pos, this.lastPos, this.vel, this.Rad, new IntVector2(0, 0), true);
                    terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                    terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                    terrainCollisionData = SharedPhysics.SlopesVertically(this.room, terrainCollisionData);
                    this.pos = terrainCollisionData.pos;
                    this.vel = terrainCollisionData.vel;
                    if (terrainCollisionData.contactPoint.x != 0)
                    {
                        this.vel.x = Mathf.Abs(vector2.x) * 0.2f * (float)(-(float)terrainCollisionData.contactPoint.x);
                        this.vel.y = this.vel.y * 0.8f;
                        flag = true;
                    }
                    if (terrainCollisionData.contactPoint.y != 0)
                    {
                        this.vel.y = Mathf.Abs(vector2.y) * 0.2f * (float)(-(float)terrainCollisionData.contactPoint.y);
                        this.vel.x = this.vel.x * 0.8f;
                        flag = true;
                    }
                }

                SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, this.room, this.lastPos, ref this.pos, this.Rad, 1, this.player, true);
                if (collisionResult.chunk != null)
                {
                    this.pos = collisionResult.collisionPoint;
                    this.stickChunk = collisionResult.chunk;
                    this.vel *= 0.25f;

                    //Eating(collisionResult);
                    if (this.stickChunk.owner is Creature)
                    {
                        (this.stickChunk.owner as Creature).SetKillTag(player.abstractCreature);
                        (this.stickChunk.owner as Creature).Violence(null, new Vector2?(this.vel * 0.6f), this.stickChunk, null, Creature.DamageType.Water, 0.001f, Random.value * 7f);
                        this.room.PlaySound((this.stickChunk.owner is Player) ? SoundID.Red_Lizard_Spit_Hit_Player : SoundID.Red_Lizard_Spit_Hit_NPC, this.pos);
                    }
                    else
                    {
                        this.stickChunk.vel += this.vel * 0.6f / Mathf.Max(1f, this.stickChunk.mass);
                        this.room.PlaySound(SoundID.Red_Lizard_Spit_Hit_NPC, this.pos);
                    }
                    flag = true;
                }
                if (flag)
                {
                    if (this.massLeft >= 1f)
                    {
                        this.massLeft = 0.99f;
                        if (this.stickChunk == null)
                        {

                        }
                    }
                }
            }

            if (this.massLeft < 1f)
            {
                this.massLeft -= Mathf.Lerp(0.5f, 1.5f, this.dissapearSpeed) / ((this.stickChunk == null) ? 30f : 120f);
            }
            if (this.massLeft <= 0f || this.pos.y < -300f)
            {
                this.Destroy();
            }
            base.Update(eu);
        }

        public Vector2 StuckPosOfSlime(int s, float timeStacker)
        {
            if ((int)this.slime[s, 3].x < 0 || (int)this.slime[s, 3].x >= this.slime.GetLength(0))
            {
                return Vector2.Lerp(this.lastPos, this.pos, timeStacker);
            }
            return Vector2.Lerp(this.slime[(int)this.slime[s, 3].x, 1], this.slime[(int)this.slime[s, 3].x, 0], timeStacker);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.TotalSprites];
            sLeaser.sprites[this.DotSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.DotSprite].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
            sLeaser.sprites[this.DotSprite].alpha = Random.value * 0.5f;
            sLeaser.sprites[this.JaggedSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.JaggedSprite].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
            sLeaser.sprites[this.JaggedSprite].alpha = Random.value * 0.5f;
            for (int i = 0; i < this.slime.GetLength(0); i++)
            {
                sLeaser.sprites[this.SlimeSprite(i)] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.SlimeSprite(i)].anchorY = 0.05f;
                sLeaser.sprites[this.SlimeSprite(i)].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[this.SlimeSprite(i)].alpha = Random.value;
            }
            this.AddToContainer(sLeaser, rCam, null);
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
            float t = Mathf.InverseLerp(30f, 6f, Vector2.Distance(this.lastPos, this.pos));
            float t2 = Mathf.InverseLerp(6f, 30f, Mathf.Lerp(Vector2.Distance(this.lastPos, this.pos), Vector2.Distance(vector, Vector2.Lerp(this.slime[0, 1], this.slime[0, 0], timeStacker)), t));
            Vector2 v = Vector3.Slerp(Custom.DirVec(this.lastPos, this.pos), Custom.DirVec(vector, Vector2.Lerp(this.slime[0, 1], this.slime[0, 0], timeStacker)), t);
            sLeaser.sprites[this.DotSprite].x = vector.x - camPos.x;
            sLeaser.sprites[this.DotSprite].y = vector.y - camPos.y;
            sLeaser.sprites[this.DotSprite].rotation = Custom.VecToDeg(v);
            sLeaser.sprites[this.DotSprite].scaleX = Mathf.Lerp(0.4f, 0.2f, t2) * this.massLeft;
            sLeaser.sprites[this.DotSprite].scaleY = Mathf.Lerp(0.3f, 0.7f, t2) * this.massLeft;
            sLeaser.sprites[this.JaggedSprite].x = vector.x - camPos.x;
            sLeaser.sprites[this.JaggedSprite].y = vector.y - camPos.y;
            sLeaser.sprites[this.JaggedSprite].rotation = Custom.VecToDeg(v);
            sLeaser.sprites[this.JaggedSprite].scaleX = Mathf.Lerp(0.6f, 0.4f, t2) * this.massLeft;
            sLeaser.sprites[this.JaggedSprite].scaleY = Mathf.Lerp(0.5f, 1f, t2) * this.massLeft;
            for (int i = 0; i < this.slime.GetLength(0); i++)
            {
                Vector2 vector2 = Vector2.Lerp(this.slime[i, 1], this.slime[i, 0], timeStacker);
                Vector2 vector3 = this.StuckPosOfSlime(i, timeStacker);
                sLeaser.sprites[this.SlimeSprite(i)].x = vector2.x - camPos.x;
                sLeaser.sprites[this.SlimeSprite(i)].y = vector2.y - camPos.y;
                sLeaser.sprites[this.SlimeSprite(i)].scaleY = (Vector2.Distance(vector2, vector3) + 3f) / 16f;
                sLeaser.sprites[this.SlimeSprite(i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                sLeaser.sprites[this.SlimeSprite(i)].scaleX = Custom.LerpMap(Vector2.Distance(vector2, vector3), 0f, this.slime[i, 3].y * 3.5f, 6f, 2f, 2f) * this.massLeft / 16f;
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[this.JaggedSprite].color = Color.Lerp(player.ShortCutColor(), palette.blackColor, 0);

            sLeaser.sprites[this.DotSprite].color = player.ShortCutColor();

            for (int i = 0; i < this.slime.GetLength(0); i++)
            {
                sLeaser.sprites[this.SlimeSprite(i)].color = Color.Lerp(player.ShortCutColor(), palette.fogColor, 0.4f * Random.value);
            }

        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }


        public Vector2 pos;
        public Vector2 lastPos;
        public Vector2 vel;
        public Player player;
        public BodyChunk stickChunk;
        public int fallOff;
        public float massLeft;
        public float dissapearSpeed;
        public Vector2[,] slime;
        public SharedPhysics.TerrainCollisionData scratchTerrainCollisionData = new SharedPhysics.TerrainCollisionData();
    }
}