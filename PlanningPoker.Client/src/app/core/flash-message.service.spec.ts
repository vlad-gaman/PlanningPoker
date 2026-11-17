import { FlashMessageService } from './flash-message.service';

describe('FlashMessageService', () => {
  it('holds a message until it is cleared', () => {
    const service = new FlashMessageService();
    expect(service.message()).toBeNull();

    service.show('Hello');
    expect(service.message()).toBe('Hello');

    service.clear();
    expect(service.message()).toBeNull();
  });
});
