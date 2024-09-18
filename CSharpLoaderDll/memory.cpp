#include "pch.h"
#include "memory.h"


memory::allocator memory::mem_allocator_;

memory::allocator::~allocator()
{
	this->clear();
}

void memory::allocator::clear()
{
	std::lock_guard<std::mutex> _(this->mutex_);

	for (auto& data : this->pool_)
	{
		memory::free(data);
	}

	this->pool_.clear();
}

void memory::allocator::free(void* data)
{
	std::lock_guard<std::mutex> _(this->mutex_);

	const auto j = std::find(this->pool_.begin(), this->pool_.end(), data);
	if (j != this->pool_.end())
	{
		memory::free(data);
		this->pool_.erase(j);
	}
}

void memory::allocator::free(const void* data)
{
	this->free(const_cast<void*>(data));
}

void* memory::allocator::allocate(const size_t length)
{
	std::lock_guard<std::mutex> _(this->mutex_);

	const auto data = memory::allocate(length);
	this->pool_.push_back(data);
	return data;
}

bool memory::allocator::empty() const
{
	return this->pool_.empty();
}

void* memory::allocate(const size_t length)
{
	return calloc(length, 1);
}

void memory::free(void* data)
{
	if (data)
	{
		::free(data);
	}
}

void memory::free(const void* data)
{
	free(const_cast<void*>(data));
}

bool memory::is_set(const void* mem, const char chr, const size_t length)
{
	const auto mem_arr = static_cast<const char*>(mem);

	for (size_t i = 0; i < length; ++i)
	{
		if (mem_arr[i] != chr)
		{
			return false;
		}
	}

	return true;
}

memory::allocator* memory::get_allocator()
{
	return &memory::mem_allocator_;
}

std::uint8_t* find_sig(LPCSTR module_name, const std::string& byte_array)
{
	HMODULE module = GetModuleHandleA(module_name);

	if (!module)
		return nullptr;

	static const auto pattern_to_byte = [](const char* pattern)
	{
		auto bytes = std::vector<int>{};
		const auto start = const_cast<char*>(pattern);
		const auto end = const_cast<char*>(pattern) + std::strlen(pattern);

		for (auto current = start; current < end; ++current)
		{
			if (*current == '?')
			{
				++current;

				if (*current == '?')
					++current;

				bytes.push_back(-1);
			}
			else
			{
				bytes.push_back(std::strtoul(current, &current, 16));
			}
		}
		return bytes;
	};

	const auto dos_header = reinterpret_cast<PIMAGE_DOS_HEADER>(module);
	const auto nt_headers =
		reinterpret_cast<PIMAGE_NT_HEADERS>(reinterpret_cast<std::uint8_t*>(module) + dos_header->e_lfanew);

	const auto size_of_image = nt_headers->OptionalHeader.SizeOfImage;
	const auto pattern_bytes = pattern_to_byte(byte_array.c_str());
	const auto scan_bytes = reinterpret_cast<std::uint8_t*>(module);

	const auto pattern_size = pattern_bytes.size();
	const auto pattern_data = pattern_bytes.data();

	for (auto i = 0ul; i < size_of_image - pattern_size; ++i)
	{
		auto found = true;

		for (auto j = 0ul; j < pattern_size; ++j)
		{
			if (scan_bytes[i + j] == pattern_data[j] || pattern_data[j] == -1)
				continue;
			found = false;
			break;
		}
		if (!found)
			continue;
		return &scan_bytes[i];
	}

	return nullptr;
}

signature::signature(const std::string& sig)
{
	this->imported = false;
	this->sig = sig;
	this->pointer = 0;
}

signature signature::import(const char* module_name)
{
	this->imported = true;
	this->pointer = (uint64_t)find_sig(module_name, this->sig);
	return *this;
}

signature signature::add(uint32_t value)
{
	if (!this->imported)
		*this = this->import();

	this->pointer += value;
	return *this;
}

signature signature::sub(uint32_t value)
{
	if (!this->imported)
		*this = this->import();

	this->pointer -= value;
	return *this;
}

signature signature::dump(const std::string& func_name)
{
	if (!this->imported)
		*this = this->import();

	return *this;
}

signature signature::instruction(uint32_t offset)
{
	if (!this->imported)
		*this = this->import();

	this->pointer = *(int*)(this->pointer + offset) + this->pointer;
	return *this;
}

#ifdef _WIN64
uint64_t signature::GetPointer()
{
	if (!this->imported)
		*this = this->import();
	return this->pointer;
}
#else
uint32_t signature::GetPointer()
{
	if (!this->imported)
		*this = this->import();
	return this->pointer;
}
#endif

static inline void memcpy_(void* _Dst, void const* _Src, size_t _Size)
{
	auto csrc = (char*)_Src;
	auto cdest = (char*)_Dst;

	for (int i = 0; i < _Size; i++)
	{
		cdest[i] = csrc[i];
	}
}

void swap_virtual_table(void* obj, uint32_t index, void* func)
{
	auto currVt = *(void**)(obj);

	auto vtable = *(void***)(obj);
	int i = 0;

	for (; vtable[i]; i++)
		__noop();

	auto newVt = new uintptr_t[i];

	memcpy_(newVt, currVt, i * 0x8);

	newVt[index] = (uintptr_t)func;

	*(uintptr_t**)(obj) = newVt;
}

bool get_key_state(int key, int flag)
{
	clock_t	c = clock();
	static BYTE			btKeyState[0x100] = { 0 };
	static clock_t		clockKeyState[0x100] = { c };

	bool	r = false;
	BYTE	btState = (GetAsyncKeyState(key) & 0x8000U) >> 0xF;
	short	wDelay = btKeyState[key] & 2 ? 0x50 : 0x200;
	if (btState) {
		if (flag & 1) {
			if (!(btKeyState[key] & 1) || c - clockKeyState[key] > wDelay)
			{
				if ((btKeyState[key] & 3) == 1)
					btKeyState[key] |= 2;
				r = true;
				clockKeyState[key] = c;
			}
		}
		else
			r = true;
	}
	else
		btKeyState[key] &= ~(2);
	btKeyState[key] ^= (-btState ^ btKeyState[key]) & 1;

	return r;
}