import type { PageLoad } from './$types';

const errorMessages: Record<string, string> = {
	'already-joined':
		'The bot has already been added to your channel. If you believe this is wrong, contact @melon095 :)',
	cooldown: "You're doing that too fast! Please wait a bit before trying again.",
	'403': 'You do not have permission to access this page.',
	'404': 'The page you are looking for does not exist.'
};

const defaultErrorMessage = 'An error occurred';

export const load: PageLoad = ({ params }) => {
	const errorCode = params.errorCode;

	const message = errorMessages[errorCode] ?? defaultErrorMessage;

	return {
		props: {
			errorCode,
			message
		}
	};
};
