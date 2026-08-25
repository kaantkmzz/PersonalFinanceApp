using PersonalFinanceApp.Data;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class NoteService
    {
        private readonly NoteRepository _repository = new NoteRepository();

        public List<Note> GetUserNotes(int userId)
        {
            return _repository.GetByUserId(userId);
        }

        public List<Note> GetRecentlyUpdatedNotes(int userId, int limit)
        {
            return _repository.GetRecentlyUpdated(userId, limit);
        }

        public bool AddNote(int userId, string title, string content, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Not başlığı boş olamaz.";
                return false;
            }

            _repository.Add(new Note { UserId = userId, Title = title, Content = content });
            return true;
        }

        public bool UpdateNote(int noteId, int userId, string title, string content, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage = "Not başlığı boş olamaz.";
                return false;
            }

            _repository.Update(noteId, userId, title, content);
            return true;
        }

        public void DeleteNote(int noteId, int userId)
        {
            _repository.Delete(noteId, userId);
        }
    }
}