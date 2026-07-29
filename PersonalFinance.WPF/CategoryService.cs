using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _repository = new CategoryRepository();

        // Yeni kullanıcı kayıt olduğunda çağrılacak — birkaç temel kategori otomatik oluşturur
        public void CreateDefaultCategories(int userId)
        {
            var defaults = new List<Category>
            {
                new Category { UserId = userId, Name = "Maaş", Type = "income" },
                new Category { UserId = userId, Name = "Ek Gelir", Type = "income" },
                new Category { UserId = userId, Name = "Market", Type = "expense" },
                new Category { UserId = userId, Name = "Fatura", Type = "expense" },
                new Category { UserId = userId, Name = "Ulaşım", Type = "expense" },
                new Category { UserId = userId, Name = "Eğlence", Type = "expense" }
            };

            foreach (var category in defaults)
            {
                _repository.Add(category);
            }
        }

        public List<Category> GetUserCategories(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public List<Category> GetUserCategoriesByType(int userId, string type)
        {
            return _repository.GetByUserId(userId).Where(c => c.Type == type).ToList();
        }

        public bool AddCategory(int userId, string name, string type, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Kategori adı boş olamaz.";
                return false;
            }

            if (type != "income" && type != "expense")
            {
                errorMessage = "Geçersiz kategori tipi.";
                return false;
            }

            var existing = _repository.GetByUserId(userId);
            if (existing.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.Type == type))
            {
                errorMessage = "Bu isimde bir kategori zaten var.";
                return false;
            }

            _repository.Add(new Category { UserId = userId, Name = name, Type = type });
            return true;
        }

        public void DeleteCategory(int categoryId, int userId)
        {
            _repository.Delete(categoryId, userId);
        }

        public bool UpdateCategoryName(int categoryId, int userId, string newName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(newName))
            {
                errorMessage = "Kategori adı boş olamaz.";
                return false;
            }

            _repository.Update(categoryId, userId, newName);
            return true;
        }
    }
}