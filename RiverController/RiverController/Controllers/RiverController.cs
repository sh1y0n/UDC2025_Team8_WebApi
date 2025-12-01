using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration; // IConfigurationのために必要
using System.Collections.Generic;
using System.Threading.Tasks;

// 【重要】namespaceは、あなたのプロジェクト名に合わせて修正してください
namespace RiverController.Controllers
{
    // APIのルート設定: [公開URL]/api/river でアクセス可能
    [ApiController]
    [Route("api/[controller]")]
    public class RiverController : ControllerBase
    {
        // Azureに登録した設定（接続文字列）を読み込むための変数
        private readonly IConfiguration _configuration;

        // コンストラクタで、設定情報（IConfiguration）をASP.NET Coreから自動で受け取る
        public RiverController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // HTTP GETリクエストに対応するメソッド
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RiverData>>> Get()
        {
            // 接続文字列 "DbConnection" をAzure App Serviceの環境設定から取得
            string? connectionString = _configuration.GetConnectionString("DbConnection");

            // 接続文字列が見つからない場合はエラーを返す
            if (string.IsNullOrEmpty(connectionString))
            {
                return StatusCode(500, "Error: DB connection string 'DbConnection' not found in Azure configuration. Check App Service Settings.");
            }

            var rivers = new List<RiverData>();

            try
            {
                // SQL Serverへの接続を開始
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // 非同期で接続を開く
                    await connection.OpenAsync();

                    // 実行するSQLクエリ
                    // 【注意】dbo.riverテーブルのデータが存在することが前提
                    string sql = "SELECT TOP 5 river_id, zone_type FROM dbo.river ORDER BY river_id";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync()) // 非同期でクエリ実行
                    {
                        while (await reader.ReadAsync()) // 非同期でデータを読み込み
                        {
                            rivers.Add(new RiverData
                            {
                                // データベースから取得したデータをクラスに格納
                                RiverId = reader["river_id"].ToString(),
                                ZoneType = reader["zone_type"].ToString()
                            });
                        }
                    }
                }

                // 成功したら、データをJSON形式で返す (HTTP 200 OK)
                return Ok(rivers);
            }
            catch (Exception ex)
            {
                // DB接続やクエリ実行でエラーが発生した場合
                // エラー内容を返して、デバッグを容易にする (HTTP 500 Internal Server Error)
                return StatusCode(500, $"Database Access Error: {ex.Message}");
            }
        }
    }

    // APIが返すJSONデータの構造を定義するクラス
    // Androidアプリ側もこの構造に合わせてデータを読み込みます
    public class RiverData
    {
        public string? RiverId { get; set; }
        public string? ZoneType { get; set; }
    }
}