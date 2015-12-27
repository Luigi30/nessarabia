#pragma once
#include "stdafx.h"
#include "CPU.h"
#include "mc6821.h"
#include "s2513.h"

class EmulatorState {
public:
	enum COMPUTER_CONFIG { NES };
	static EmulatorState* Instance();
	CPU processor = CPU();
	
	void init();
	COMPUTER_CONFIG EmulatorState::getConfigurationType();

	void (__stdcall *ppuUpdatedCallback)();

private:
	EmulatorState() {};
	EmulatorState(EmulatorState const&) {};
	EmulatorState& operator=(EmulatorState const&) {};
	static EmulatorState* m_pInstance;
	COMPUTER_CONFIG config = NES;
};