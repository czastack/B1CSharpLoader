#pragma once
#include <Windows.h>
#include <mutex>
#include <vector>

std::uint8_t* find_sig(LPCSTR module_name, const std::string& byte_array);

class memory final
{
public:
	class allocator final
	{
	public:
		~allocator();

		void clear();

		void free(void* data);

		void free(const void* data);

		void* allocate(size_t length);

		template <typename T>
		T* allocate()
		{
			return this->allocate_array<T>(1);
		}

		template <typename T>
		T* allocate_array(const size_t count = 1)
		{
			auto base = static_cast<T*>(this->allocate(count * sizeof(T)));

			T();
			return base;
		}

		bool empty() const;

	private:
		std::mutex mutex_;
		std::vector<void*> pool_;
	};

	static void* allocate(size_t length);

	template <typename T>
	static inline T* allocate()
	{
		return allocate_array<T>(1);
	}

	template <typename T>
	static inline T* allocate_array(const size_t count = 1)
	{
		return static_cast<T*>(allocate(count * sizeof(T)));
	}

	static void free(void* data);
	static void free(const void* data);

	static bool is_set(const void* mem, char chr, size_t length);

	static allocator* get_allocator();

private:
	static allocator mem_allocator_;
};

class signature
{
public:

	signature(const std::string& sig);

	signature import(const char* module_name = NULL);

	signature add(uint32_t offset);
	signature sub(uint32_t offset);
	signature instruction(uint32_t offset);

	signature dump(const std::string& func_name);

#ifdef _WIN64
	uint64_t GetPointer();
#else
	uint32_t GetPointer();
#endif

private:
#ifdef _WIN64
	uint64_t pointer;
#else
	uint32_t pointer;
#endif

	bool imported;
	std::string sig;

};

void swap_virtual_table(void* obj, uint32_t index, void* func);

bool get_key_state(int key, int flag = 1);
