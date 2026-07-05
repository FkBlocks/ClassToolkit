using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClassToolkit.Core.Services
{
    /// <summary>
    /// 配置读写服务。内部用 JsonObject（等价于 Python dict）存储所有键值对，
    /// 添加新配置只需改 JSON 和 UI 控件，无需修改 Model。
    /// </summary>
    public class ConfigService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly string _configPath;

        public ConfigService(string? configPath = null)
        {
            _configPath = configPath ?? ClassToolkit.Core.Utilities.DataPathHelper.GetDataPath("config/config.json");
        }

        /// <summary>
        /// 加载整个 config.json 为 JsonObject 字典。
        /// 文件不存在或损坏时返回空字典。
        /// </summary>
        public JsonObject Load()
        {

            if (!System.IO.File.Exists(_configPath))
                return new JsonObject();

            try
            {
                string json = System.IO.File.ReadAllText(_configPath);
                return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                return new JsonObject();
            }
        }

        /// <summary>
        /// 若没有当前配置，则自动在json中创建并添加默认值
        /// </summary>
        /// <param name="defaults">无则添加的默认配置</param>
        /// <returns></returns>
        public JsonObject Load(Dictionary<string, object?> defaults)
        {
            var config = Load();
            bool changed = false;

            foreach (var kv in defaults)
            {
                if (!config.ContainsKey(kv.Key))
                {
                    config[kv.Key] = JsonValue.Create(kv.Value);
                    changed = true;
                }
            }

            if (changed)
            {
                Save(config);
            }
            return config;
        }

        /// <summary>
        /// 将 JsonObject 写回 config.json。目录不存在时自动创建。
        /// </summary>
        public void Save(JsonObject config)
        {
            string? dir = System.IO.Path.GetDirectoryName(_configPath);
            if (dir != null)
                System.IO.Directory.CreateDirectory(dir);

            string json = config.ToJsonString(JsonOptions);
            System.IO.File.WriteAllText(_configPath, json);
        }
    }
}
