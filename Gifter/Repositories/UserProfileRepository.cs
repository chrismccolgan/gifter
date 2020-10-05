using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Gifter.Models;
using Gifter.Utils;

namespace Gifter.Repositories
{
    public class UserProfileRepository : BaseRepository, IUserProfileRepository
    {
        public UserProfileRepository(IConfiguration configuration) : base(configuration) { }

        public List<UserProfile> GetAll()
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, Bio, Email, DateCreated AS UserProfileDateCreated, 
                               ImageUrl AS UserProfileImageUrl
                          FROM UserProfile";

                    var reader = cmd.ExecuteReader();

                    var userProfiles = new List<UserProfile>();
                    while (reader.Read())
                    {
                        userProfiles.Add(new UserProfile()
                        {
                            Id = DbUtils.GetInt(reader, "Id"),
                            Name = DbUtils.GetString(reader, "Name"),
                            Email = DbUtils.GetString(reader, "Email"),
                            DateCreated = DbUtils.GetDateTime(reader, "UserProfileDateCreated"),
                            ImageUrl = DbUtils.GetString(reader, "UserProfileImageUrl"),
                            Bio = DbUtils.GetString(reader, "Bio")
                        });
                    }

                    reader.Close();

                    return userProfiles;
                }
            }
        }

        public UserProfile GetById(int id)
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, Bio, Email, DateCreated AS UserProfileDateCreated, 
                               ImageUrl AS UserProfileImageUrl
                          FROM UserProfile
                         WHERE Id = @Id";

                    DbUtils.AddParameter(cmd, "@Id", id);

                    var reader = cmd.ExecuteReader();

                    UserProfile userProfile = null;
                    if (reader.Read())
                    {
                        userProfile = new UserProfile()
                        {
                            Id = id,
                            Name = DbUtils.GetString(reader, "Name"),
                            Email = DbUtils.GetString(reader, "Email"),
                            DateCreated = DbUtils.GetDateTime(reader, "UserProfileDateCreated"),
                            ImageUrl = DbUtils.GetString(reader, "UserProfileImageUrl"),
                            Bio = DbUtils.GetString(reader, "Bio")
                        };
                    }

                    reader.Close();

                    return userProfile;
                }
            }
        }

        public void Add(UserProfile userProfile)
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO UserProfile (Name, Email, ImageUrl, Bio, DateCreated)
                        OUTPUT INSERTED.ID
                        VALUES (@Name, @Email, @ImageUrl, @Bio, @DateCreated)";

                    DbUtils.AddParameter(cmd, "@Name", userProfile.Name);
                    DbUtils.AddParameter(cmd, "@Email", userProfile.Email);
                    DbUtils.AddParameter(cmd, "@ImageUrl", userProfile.ImageUrl);
                    DbUtils.AddParameter(cmd, "@Bio", userProfile.Bio);
                    DbUtils.AddParameter(cmd, "@DateCreated", userProfile.DateCreated);

                    userProfile.Id = (int)cmd.ExecuteScalar();
                }
            }
        }

        public void Update(UserProfile userProfile)
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE UserProfile
                           SET Name = @Name,
                               Email = @Email,
                               ImageUrl = @ImageUrl,
                               Bio = @Bio,
                               DateCreated = @DateCreated
                         WHERE Id = @Id";

                    DbUtils.AddParameter(cmd, "@Name", userProfile.Name);
                    DbUtils.AddParameter(cmd, "@Email", userProfile.Email);
                    DbUtils.AddParameter(cmd, "@ImageUrl", userProfile.ImageUrl);
                    DbUtils.AddParameter(cmd, "@Bio", userProfile.Bio);
                    DbUtils.AddParameter(cmd, "@DateCreated", userProfile.DateCreated);
                    DbUtils.AddParameter(cmd, "@Id", userProfile.Id);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM UserProfile WHERE Id = @Id";
                    DbUtils.AddParameter(cmd, "@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public UserProfile GetByIdExpand(int id)
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT up.Id, up.Name, up.Bio, up.Email, up.DateCreated AS UserProfileDateCreated, 
                               up.ImageUrl AS UserProfileImageUrl,

                               p.Id AS PostId, p.Title, p.Caption, p.DateCreated AS PostDateCreated,
                               p.ImageUrl AS PostImageUrl, p.UserProfileId AS PostUserProfileId, 

                               c.Id AS CommentId, c.PostId AS CommentPostId, c.Message, c.UserProfileId AS CommentUserProfileId
                          FROM UserProfile up
                               LEFT JOIN Post p ON up.Id = p.UserProfileId
                               LEFT JOIN Comment c ON p.Id = c.PostId
                         WHERE up.Id = @Id";

                    DbUtils.AddParameter(cmd, "@Id", id);
                    var reader = cmd.ExecuteReader();

                    UserProfile userProfile = null;
                    while (reader.Read())
                    {
                        if (userProfile == null)
                        {
                            userProfile = new UserProfile()
                            {
                                Id = id,
                                Name = DbUtils.GetString(reader, "Name"),
                                Email = DbUtils.GetString(reader, "Email"),
                                DateCreated = DbUtils.GetDateTime(reader, "UserProfileDateCreated"),
                                ImageUrl = DbUtils.GetString(reader, "UserProfileImageUrl"),
                                Bio = DbUtils.GetString(reader, "Bio"),
                                Posts = new List<Post>()
                            };
                        }

                        if (DbUtils.IsNotDbNull(reader, "PostId"))
                        {
                            var postId = DbUtils.GetInt(reader, "PostId");

                            if (!userProfile.Posts.Any(p => p.Id == postId))
                            {
                                userProfile.Posts.Add(new Post()
                                {
                                    Id = postId,
                                    Title = DbUtils.GetString(reader, "Title"),
                                    Caption = DbUtils.GetString(reader, "Caption"),
                                    DateCreated = DbUtils.GetDateTime(reader, "PostDateCreated"),
                                    ImageUrl = DbUtils.GetString(reader, "PostImageUrl"),
                                    UserProfileId = DbUtils.GetInt(reader, "PostUserProfileId"),
                                    Comments = new List<Comment>()
                                });
                            }
                        }

                        if (DbUtils.IsNotDbNull(reader, "CommentId"))
                        {
                            var postId = DbUtils.GetInt(reader, "PostId");
                            var post = userProfile.Posts.First(p => p.Id == postId);

                            post.Comments.Add(new Comment()
                            {
                                Id = DbUtils.GetInt(reader, "CommentId"),
                                Message = DbUtils.GetString(reader, "Message"),
                                PostId = post.Id,
                                UserProfileId = DbUtils.GetInt(reader, "CommentUserProfileId")
                            });
                        }
                    }
                    
                    reader.Close();

                    return userProfile;
                }
            }
        }
    }
}