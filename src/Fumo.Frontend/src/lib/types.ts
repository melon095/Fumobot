export type Command = {
	name: string;
	description: string;
	cooldown: number;
};

export type CommandModal = {
	regex: string;
	permission: string;
	description: string;
};
